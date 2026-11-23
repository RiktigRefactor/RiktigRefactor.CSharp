using Microsoft.CodeAnalysis.Text;
using RiktigRefactor.CSharp.Protocol;
using RiktigRefactor.CSharp.Verification;

namespace RiktigRefactor.CSharp.Tests.Infrastructure;

/// <summary>Helpers shared by the case runners of every refactoring.</summary>
public static class CaseFiles
{
    /// <summary>Set to 1 to write the produced output into each case's expected directory. Review the diff before committing.</summary>
    const string WriteExpectedVariable = "RIKTIG_WRITE_EXPECTED";

    public static string ApplyEdits(string text, IEnumerable<FileEdit> edits)
    {
        var sourceText = SourceText.From(text);
        var changes = edits.Select(edit => new TextChange(
            TextSpan.FromBounds(Ranges.ToOffset(sourceText, edit.Start), Ranges.ToOffset(sourceText, edit.End)),
            edit.NewText));
        return sourceText.WithChanges(changes).ToString();
    }

    public static void WriteExpectedIfRequested(string name, IReadOnlyDictionary<string, string> outputFiles)
    {
        if (Environment.GetEnvironmentVariable(WriteExpectedVariable) != "1")
        {
            return;
        }

        var directory = TestCase.ExpectedDirectory(name);
        foreach (var (file, text) in outputFiles)
        {
            var path = Path.Combine(directory, file);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text);
        }
    }
}
