using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using RiktigRefactor.CSharp.Protocol;
using RiktigRefactor.CSharp.Rename;
using RiktigRefactor.CSharp.Verification;

namespace RiktigRefactor.CSharp.Tool;

public static class RenameCommand
{
    public const int ExitSuccess = 0;
    public const int ExitRefused = 1;
    public const int ExitError = 2;

    static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static async Task<int> RunAsync(RenameArguments arguments, TextWriter standardOutput, TextWriter standardError, CancellationToken cancellationToken)
    {
        try
        {
            return await RunCoreAsync(arguments, standardOutput, standardError, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is WorkspaceLoadException or ArgumentException or IOException or UnauthorizedAccessException)
        {
            await standardError.WriteLineAsync(exception.Message).ConfigureAwait(false);
            return ExitError;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Without this, System.CommandLine's default handler exits with 1, which means "refused".
            await standardError.WriteLineAsync($"Internal error: {exception}").ConfigureAwait(false);
            return ExitError;
        }
    }

    static async Task<int> RunCoreAsync(RenameArguments arguments, TextWriter standardOutput, TextWriter standardError, CancellationToken cancellationToken)
    {
        var position = ParsePosition(arguments.At);
        var solution = await WorkspaceLoader.LoadAsync(Path.GetFullPath(arguments.Workspace), cancellationToken).ConfigureAwait(false);
        var fullPath = Path.GetFullPath(arguments.File);
        var document = solution.Projects.SelectMany(project => project.Documents)
            .FirstOrDefault(candidate => candidate.FilePath is { } path && Path.GetFullPath(path) == fullPath)
            ?? throw new ArgumentException($"`{arguments.File}` is not part of the workspace.");

        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var offset = Ranges.ToOffset(text, position);
        var result = await RenameRefactoring.RenameAsync(solution, document.Id, offset, arguments.NewName, cancellationToken).ConfigureAwait(false);

        if (arguments.Json)
        {
            await standardOutput.WriteLineAsync(JsonSerializer.Serialize(result.WithWorkspaceRelativePaths(WorkspaceRootOf(arguments.Workspace)), JsonOptions)).ConfigureAwait(false);
        }
        else
        {
            await WriteTextAsync(result, solution, standardOutput, standardError, cancellationToken).ConfigureAwait(false);
        }

        if (result.Status == RefactoringStatus.Refused)
        {
            return ExitRefused;
        }

        if (arguments.Apply)
        {
            await ApplyAsync(solution, result.NewSolution!, cancellationToken).ConfigureAwait(false);
        }

        return ExitSuccess;
    }

    static string WorkspaceRootOf(string workspace) => Path.GetDirectoryName(Path.GetFullPath(workspace))!;

    static Position ParsePosition(string at)
    {
        var parts = at.Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var line) || !int.TryParse(parts[1], out var column) || line < 1 || column < 1)
        {
            throw new ArgumentException($"`{at}` is not a position. Use line:column, both starting at 1.");
        }

        return new Position(line, column);
    }

    static async Task WriteTextAsync(
        RefactoringResult result,
        Solution oldSolution,
        TextWriter standardOutput,
        TextWriter standardError,
        CancellationToken cancellationToken)
    {
        foreach (var reason in result.Reasons)
        {
            await standardError.WriteLineAsync($"Refused ({reason.Code}): {reason.Message}{Describe(reason.Location)}").ConfigureAwait(false);
            if (reason.Suggestion is not null)
            {
                await standardError.WriteLineAsync($"  {reason.Suggestion}").ConfigureAwait(false);
            }
        }

        foreach (var warning in result.Warnings)
        {
            await standardError.WriteLineAsync($"Warning ({warning.Code}): {warning.Message}{Describe(warning.Location)}").ConfigureAwait(false);
        }

        if (result.NewSolution is null)
        {
            return;
        }

        foreach (var (oldDocument, newDocument) in ChangedDocuments(oldSolution, result.NewSolution))
        {
            var oldText = (await oldDocument.GetTextAsync(cancellationToken).ConfigureAwait(false)).ToString();
            var newText = (await newDocument.GetTextAsync(cancellationToken).ConfigureAwait(false)).ToString();
            await standardOutput.WriteAsync(UnifiedDiff.Format(DisplayPath(oldDocument), oldText, newText)).ConfigureAwait(false);
        }
    }

    static string Describe(FileRange? location)
        => location is null ? string.Empty : $" ({DisplayPath(location.File)}:{location.Start.Line}:{location.Start.Column})";

    static string DisplayPath(Document document) => DisplayPath(document.FilePath ?? document.Name);

    static string DisplayPath(string path)
    {
        var relative = Path.GetRelativePath(Directory.GetCurrentDirectory(), path);
        return (relative.StartsWith("..", StringComparison.Ordinal) ? path : relative).Replace('\\', '/');
    }

    static IEnumerable<(Document Old, Document New)> ChangedDocuments(Solution oldSolution, Solution newSolution)
        => newSolution.GetChanges(oldSolution).GetProjectChanges()
            .SelectMany(changes => changes.GetChangedDocuments(onlyGetDocumentsWithTextChanges: true))
            .Select(id => (oldSolution.GetDocument(id)!, newSolution.GetDocument(id)!));

    /// <summary>Prepares every file before writing any, so that a file that can't be rewritten leaves the workspace untouched.</summary>
    static async Task ApplyAsync(Solution oldSolution, Solution newSolution, CancellationToken cancellationToken)
    {
        var writes = new Dictionary<string, (string Text, Encoding Encoding)>(StringComparer.Ordinal);
        foreach (var (oldDocument, newDocument) in ChangedDocuments(oldSolution, newSolution))
        {
            var path = oldDocument.FilePath ?? throw new ArgumentException($"{oldDocument.Name} has no file path.");
            var text = await newDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
            writes[path] = (text.ToString(), DetectEncoding(path));
        }

        foreach (var (path, (text, encoding)) in writes)
        {
            await File.WriteAllTextAsync(path, text, encoding, CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <summary>Finds the encoding to write back with, so that a file keeps its byte order mark (or lack of one). Only Unicode encodings are supported.</summary>
    static Encoding DetectEncoding(string path)
    {
        var bytes = File.ReadAllBytes(path);
        return bytes switch
        {
            [0xEF, 0xBB, 0xBF, ..] => new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
            [0xFF, 0xFE, ..] => new UnicodeEncoding(bigEndian: false, byteOrderMark: true),
            [0xFE, 0xFF, ..] => new UnicodeEncoding(bigEndian: true, byteOrderMark: true),
            _ => IsValidUtf8(bytes)
                ? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
                : throw new ArgumentException($"`{path}` is not UTF-8 or UTF-16, so it can't be rewritten safely."),
        };
    }

    static bool IsValidUtf8(byte[] bytes)
    {
        try
        {
            _ = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(bytes);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }
}
