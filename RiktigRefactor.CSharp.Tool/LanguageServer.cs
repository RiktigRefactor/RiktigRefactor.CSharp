using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using RiktigRefactor.CSharp.Rename;
using RiktigRefactor.CSharp.Verification;

namespace RiktigRefactor.CSharp.Tool;

/// <summary>
/// A minimal language server (LSP over a stream pair) that offers Rename as a code action and runs it on request.
/// It reads files from disk, so the client saves modified files before running a refactoring, and it reloads the workspace for every request.
/// </summary>
public class LanguageServer
{
    public const string RefactorCommand = "riktig.refactor";
    const string RenameKind = "refactor.rewrite.riktig.rename";
    const int MethodNotFound = -32601;
    const int InvalidRequest = -32600;
    const int InternalError = -32603;
    const int ServerNotInitialized = -32002;

    readonly Stream _input;
    readonly Stream _output;
    string? _workspacePath;
    string? _workspaceFolder;
    bool _initialized;
    bool _shutdownRequested;

    public LanguageServer(Stream input, Stream output)
    {
        this._input = input;
        this._output = output;
    }

    /// <summary>Serves requests until the client sends <c>exit</c> or closes the stream. Returns the process exit code the LSP specification asks for.</summary>
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        while (await ReadMessageAsync(cancellationToken).ConfigureAwait(false) is { } message)
        {
            if (message["method"]?.GetValue<string>() == "exit")
            {
                return this._shutdownRequested ? 0 : 1;
            }

            await HandleAsync(message, cancellationToken).ConfigureAwait(false);
        }

        return 1;
    }

    async Task HandleAsync(JsonNode message, CancellationToken cancellationToken)
    {
        var method = message["method"]?.GetValue<string>();
        var id = message["id"]?.DeepClone();
        if (method is null)
        {
            return;
        }

        if (id is null)
        {
            return;
        }

        try
        {
            var result = await HandleRequestAsync(method, message["params"], cancellationToken).ConfigureAwait(false);
            await WriteMessageAsync(new JsonObject { ["jsonrpc"] = "2.0", ["id"] = id, ["result"] = result }).ConfigureAwait(false);
        }
        catch (LanguageServerException exception)
        {
            await WriteErrorAsync(id, exception.Code, exception.Message).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is WorkspaceLoadException or ArgumentException or IOException or UnauthorizedAccessException)
        {
            await WriteErrorAsync(id, InternalError, exception.Message).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await WriteErrorAsync(id, InternalError, $"Internal error: {exception}").ConfigureAwait(false);
        }
    }

    Task<JsonNode?> HandleRequestAsync(string method, JsonNode? parameters, CancellationToken cancellationToken)
    {
        if (method != "initialize" && !this._initialized)
        {
            throw new LanguageServerException(ServerNotInitialized, "The server has not been initialized.");
        }

        return method switch
        {
            "initialize" => Task.FromResult<JsonNode?>(Initialize(parameters)),
            "shutdown" => Task.FromResult(Shutdown()),
            "textDocument/codeAction" => Task.FromResult<JsonNode?>(CodeActions(parameters)),
            "workspace/executeCommand" => ExecuteCommandAsync(parameters, cancellationToken),
            _ => throw new LanguageServerException(MethodNotFound, $"Unsupported request {method}."),
        };
    }

    JsonNode Initialize(JsonNode? parameters)
    {
        this._workspaceFolder = FolderOf(parameters);
        var configured = parameters?["initializationOptions"]?["workspace"]?.GetValue<string>();
        this._workspacePath = configured is null ? FindWorkspaceFile(this._workspaceFolder) : Path.GetFullPath(configured, this._workspaceFolder ?? Directory.GetCurrentDirectory());
        this._initialized = true;
        return new JsonObject
        {
            ["capabilities"] = new JsonObject
            {
                ["codeActionProvider"] = new JsonObject { ["codeActionKinds"] = new JsonArray(RenameKind) },
                ["executeCommandProvider"] = new JsonObject { ["commands"] = new JsonArray(RefactorCommand) },
            },
            ["serverInfo"] = new JsonObject { ["name"] = "riktig-cs" },
        };
    }

    JsonNode? Shutdown()
    {
        this._shutdownRequested = true;
        return null;
    }

    static string? FolderOf(JsonNode? initializeParameters)
    {
        var uri = initializeParameters?["workspaceFolders"]?[0]?["uri"]?.GetValue<string>() ?? initializeParameters?["rootUri"]?.GetValue<string>();
        return uri is null ? null : new Uri(uri).LocalPath;
    }

    static string? FindWorkspaceFile(string? folder)
    {
        if (folder is null)
        {
            return null;
        }

        string[] patterns = ["*.sln", "*.slnx", "*.csproj"];
        return patterns
            .SelectMany(pattern => Directory.EnumerateFiles(folder, pattern).Order(StringComparer.Ordinal))
            .FirstOrDefault();
    }

    static JsonArray CodeActions(JsonNode? parameters)
    {
        var only = parameters?["context"]?["only"]?.AsArray().Select(kind => kind!.GetValue<string>()).ToList();
        var requested = only is null || only.Any(kind => RenameKind == kind || RenameKind.StartsWith(kind + ".", StringComparison.Ordinal));
        if (!requested)
        {
            return [];
        }

        return
        [
            new JsonObject
            {
                ["title"] = "Riktig Rename",
                ["kind"] = RenameKind,
                ["command"] = new JsonObject
                {
                    ["title"] = "Riktig Rename",
                    ["command"] = "riktigrefactor.rename",
                    ["arguments"] = new JsonArray(parameters?["textDocument"]?["uri"]?.DeepClone(), parameters?["range"]?.DeepClone()),
                },
            },
        ];
    }

    async Task<JsonNode?> ExecuteCommandAsync(JsonNode? parameters, CancellationToken cancellationToken)
    {
        if (parameters?["command"]?.GetValue<string>() != RefactorCommand)
        {
            throw new LanguageServerException(MethodNotFound, $"Unsupported command {parameters?["command"]}.");
        }

        var request = parameters["arguments"]?[0] ?? throw new LanguageServerException(InvalidRequest, "The command needs a protocol request as its argument.");
        var refactoring = request["refactoring"]?.GetValue<string>();
        if (refactoring != "rename")
        {
            throw new LanguageServerException(InvalidRequest, $"Unsupported refactoring {refactoring}.");
        }

        return await RenameAsync(request, cancellationToken).ConfigureAwait(false);
    }

    async Task<JsonNode?> RenameAsync(JsonNode request, CancellationToken cancellationToken)
    {
        var workspacePath = request["workspace"]?.GetValue<string>() ?? this._workspacePath
            ?? throw new LanguageServerException(InvalidRequest, "No .sln, .slnx or .csproj was found in the workspace folder. Open a folder that contains one, or set the riktigrefactor.csharp.workspace setting.");
        var workspaceRoot = RefactoringCommand.WorkspaceRootOf(workspacePath);
        var newName = request["options"]?["newName"]?.GetValue<string>() ?? throw new LanguageServerException(InvalidRequest, "Rename needs options.newName.");
        var location = request["location"] ?? throw new LanguageServerException(InvalidRequest, "Rename needs a location.");
        var file = Path.GetFullPath(location["file"]!.GetValue<string>(), workspaceRoot);
        var start = new Protocol.Position(location["start"]!["line"]!.GetValue<int>(), location["start"]!["column"]!.GetValue<int>());

        var solution = await WorkspaceLoader.LoadAsync(Path.GetFullPath(workspacePath), cancellationToken).ConfigureAwait(false);
        var document = RefactoringCommand.FindDocument(solution, file);
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var result = await RenameRefactoring.RenameAsync(solution, document.Id, Ranges.ToOffset(text, start), newName, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.SerializeToNode(result.WithWorkspaceRelativePaths(workspaceRoot), RefactoringCommand.JsonOptions);
    }

    async Task<JsonNode?> ReadMessageAsync(CancellationToken cancellationToken)
    {
        var contentLength = -1;
        while (await ReadHeaderLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            if (line.Length == 0)
            {
                break;
            }

            if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
            {
                contentLength = int.Parse(line["Content-Length:".Length..].Trim(), System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        if (contentLength < 0)
        {
            return null;
        }

        var body = new byte[contentLength];
        await this._input.ReadExactlyAsync(body, cancellationToken).ConfigureAwait(false);
        return JsonNode.Parse(body);
    }

    /// <summary>Reads one header line, or returns null at the end of the stream.</summary>
    async Task<string?> ReadHeaderLineAsync(CancellationToken cancellationToken)
    {
        var bytes = new List<byte>();
        var single = new byte[1];
        while (await this._input.ReadAsync(single, cancellationToken).ConfigureAwait(false) == 1)
        {
            if (single[0] == (byte)'\n')
            {
                return Encoding.ASCII.GetString([.. bytes]).TrimEnd('\r');
            }

            bytes.Add(single[0]);
        }

        return null;
    }

    Task WriteErrorAsync(JsonNode id, int code, string message)
        => WriteMessageAsync(new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["error"] = new JsonObject { ["code"] = code, ["message"] = message },
        });

    async Task WriteMessageAsync(JsonNode message)
    {
        var body = Encoding.UTF8.GetBytes(message.ToJsonString());
        var header = Encoding.ASCII.GetBytes($"Content-Length: {body.Length}\r\n\r\n");
        await this._output.WriteAsync(header).ConfigureAwait(false);
        await this._output.WriteAsync(body).ConfigureAwait(false);
        await this._output.FlushAsync().ConfigureAwait(false);
    }

    class LanguageServerException(int code, string message) : Exception(message)
    {
        public int Code { get; } = code;
    }
}
