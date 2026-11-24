using System.Text;
using System.Text.Json.Nodes;
using RiktigRefactor.CSharp.Tool;

namespace RiktigRefactor.CSharp.Tests;

/// <summary>Runs the language server over in-memory pipes against a small project on disk.</summary>
public class LanguageServerTests : IDisposable
{
    const string ProjectFile = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <OutputType>Exe</OutputType>
            <TargetFramework>net10.0</TargetFramework>
          </PropertyGroup>
        </Project>
        """;

    const string CalculatorSource = "static class Calculator\n{\n    public static int Double(int value) => value * 2;\n}\n";

    const string ProgramSource = "class Program\n{\n    static void Main()\n    {\n        System.Console.WriteLine(Calculator.Double(21));\n    }\n}\n";

    readonly string _directory = Directory.CreateTempSubdirectory("riktig-cs-lsp-").FullName;

    public void Dispose()
    {
        Directory.Delete(this._directory, recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task InitializeAdvertisesCodeActionsAndTheRefactorCommand()
    {
        var responses = await ConverseAsync([Initialize(1), Exit()]);

        var capabilities = responses[0]["result"]!["capabilities"]!;
        Assert.Equal("refactor.rewrite.riktig.rename", capabilities["codeActionProvider"]!["codeActionKinds"]![0]!.GetValue<string>());
        Assert.Equal("riktig.refactor", capabilities["executeCommandProvider"]!["commands"]![0]!.GetValue<string>());
    }

    [Fact]
    public async Task CodeActionOffersRenameAsAClientCommandWithTheDocumentAndRange()
    {
        var range = new JsonObject { ["start"] = new JsonObject { ["line"] = 2, ["character"] = 22 }, ["end"] = new JsonObject { ["line"] = 2, ["character"] = 22 } };
        var responses = await ConverseAsync([
            Initialize(1),
            Request(2, "textDocument/codeAction", new JsonObject { ["textDocument"] = new JsonObject { ["uri"] = "file:///x/Calculator.cs" }, ["range"] = range, ["context"] = new JsonObject { ["diagnostics"] = new JsonArray() } }),
            Exit(),
        ]);

        var action = responses[1]["result"]![0]!;
        Assert.Equal("refactor.rewrite.riktig.rename", action["kind"]!.GetValue<string>());
        Assert.Equal("riktigrefactor.rename", action["command"]!["command"]!.GetValue<string>());
        Assert.Equal("file:///x/Calculator.cs", action["command"]!["arguments"]![0]!.GetValue<string>());
        Assert.Equal(22, action["command"]!["arguments"]![1]!["start"]!["character"]!.GetValue<int>());
    }

    [Fact]
    public async Task CodeActionRespectsTheRequestedKinds()
    {
        var responses = await ConverseAsync([
            Initialize(1),
            Request(2, "textDocument/codeAction", new JsonObject { ["context"] = new JsonObject { ["only"] = new JsonArray("quickfix") } }),
            Exit(),
        ]);

        Assert.Empty(responses[1]["result"]!.AsArray());
    }

    [Fact]
    public async Task ExecutingRenameReturnsTheProtocolResultAndWritesNothing()
    {
        WriteProject();

        var responses = await ConverseAsync([Initialize(1), RenameRequest(2, "Twice"), Exit()]);

        var result = responses[1]["result"]!;
        Assert.Equal("success", result["status"]!.GetValue<string>());
        Assert.Equal(Path.GetDirectoryName(PathOf("Sample.csproj")), result["workspaceRoot"]!.GetValue<string>());
        var edits = result["edits"]!.AsArray();
        Assert.Equal(2, edits.Count);
        Assert.Equal("Twice", edits[0]!["newText"]!.GetValue<string>());
        Assert.Matches(@"^(Calculator|Program)\.cs$", edits[0]!["file"]!.GetValue<string>());
        Assert.Equal(CalculatorSource, File.ReadAllText(PathOf("Calculator.cs")));
    }

    [Fact]
    public async Task ARefusalIsAProtocolResultNotAnError()
    {
        WriteProject();

        var responses = await ConverseAsync([Initialize(1), RenameRequest(2, "class"), Exit()]);

        var result = responses[1]["result"]!;
        Assert.Equal("refused", result["status"]!.GetValue<string>());
        Assert.Equal("invalid-identifier", result["reasons"]![0]!["code"]!.GetValue<string>());
    }

    [Fact]
    public async Task ExecutingWithoutAWorkspaceFileIsAnErrorThatSaysWhatIsMissing()
    {
        var responses = await ConverseAsync([Initialize(1), RenameRequest(2, "Twice"), Exit()]);

        Assert.Contains(".csproj", responses[1]["error"]!["message"]!.GetValue<string>());
    }

    [Fact]
    public async Task RequestsBeforeInitializeAreRejected()
    {
        var responses = await ConverseAsync([RenameRequest(1, "Twice"), Exit()]);

        Assert.Equal(-32002, responses[0]["error"]!["code"]!.GetValue<int>());
    }

    [Fact]
    public async Task UnknownRequestsGetMethodNotFound()
    {
        var responses = await ConverseAsync([Initialize(1), Request(2, "textDocument/hover", new JsonObject()), Exit()]);

        Assert.Equal(-32601, responses[1]["error"]!["code"]!.GetValue<int>());
    }

    [Fact]
    public async Task ExitAfterShutdownReturnsZeroAndExitWithoutItReturnsOne()
    {
        var clean = await RunAsync([Initialize(1), Request(2, "shutdown", null), Exit()]);
        var abrupt = await RunAsync([Initialize(1), Exit()]);

        Assert.Equal(0, clean.ExitCode);
        Assert.Equal(1, abrupt.ExitCode);
    }

    string PathOf(string name) => Path.Combine(this._directory, name);

    void WriteProject()
    {
        File.WriteAllText(PathOf("Sample.csproj"), ProjectFile);
        File.WriteAllText(PathOf("Calculator.cs"), CalculatorSource);
        File.WriteAllText(PathOf("Program.cs"), ProgramSource);
    }

    JsonObject Initialize(int id)
        => Request(id, "initialize", new JsonObject { ["rootUri"] = new Uri(this._directory + Path.DirectorySeparatorChar).AbsoluteUri, ["capabilities"] = new JsonObject() });

    JsonObject RenameRequest(int id, string newName)
        => Request(id, "workspace/executeCommand", new JsonObject
        {
            ["command"] = "riktig.refactor",
            ["arguments"] = new JsonArray(new JsonObject
            {
                ["refactoring"] = "rename",
                ["location"] = new JsonObject
                {
                    ["file"] = "Calculator.cs",
                    ["start"] = new JsonObject { ["line"] = 3, ["column"] = 23 },
                    ["end"] = new JsonObject { ["line"] = 3, ["column"] = 23 },
                },
                ["options"] = new JsonObject { ["newName"] = newName },
            }),
        });

    static JsonObject Request(int id, string method, JsonObject? parameters)
        => new() { ["jsonrpc"] = "2.0", ["id"] = id, ["method"] = method, ["params"] = parameters };

    static JsonObject Exit() => new() { ["jsonrpc"] = "2.0", ["method"] = "exit" };

    static async Task<List<JsonNode>> ConverseAsync(IEnumerable<JsonObject> messages)
        => (await RunAsync(messages)).Responses;

    static async Task<(int ExitCode, List<JsonNode> Responses)> RunAsync(IEnumerable<JsonObject> messages)
    {
        using var input = new MemoryStream();
        foreach (var message in messages)
        {
            var body = Encoding.UTF8.GetBytes(message.ToJsonString());
            input.Write(Encoding.ASCII.GetBytes($"Content-Length: {body.Length}\r\n\r\n"));
            input.Write(body);
        }

        input.Position = 0;
        using var output = new MemoryStream();
        var exitCode = await new LanguageServer(input, output).RunAsync(TestContext.Current.CancellationToken);
        return (exitCode, ReadResponses(output.ToArray()));
    }

    static List<JsonNode> ReadResponses(byte[] bytes)
    {
        var responses = new List<JsonNode>();
        var offset = 0;
        while (offset < bytes.Length)
        {
            var headerEnd = Encoding.ASCII.GetString(bytes, offset, bytes.Length - offset).IndexOf("\r\n\r\n", StringComparison.Ordinal);
            var header = Encoding.ASCII.GetString(bytes, offset, headerEnd);
            var length = int.Parse(header["Content-Length: ".Length..], System.Globalization.CultureInfo.InvariantCulture);
            offset += headerEnd + 4;
            responses.Add(JsonNode.Parse(bytes.AsSpan(offset, length))!);
            offset += length;
        }

        return responses;
    }
}
