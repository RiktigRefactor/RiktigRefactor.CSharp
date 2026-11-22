using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RiktigRefactor.CSharp.Protocol;
using RiktigRefactor.CSharp.Rename;
using RiktigRefactor.CSharp.Tests.Infrastructure;
using RiktigRefactor.CSharp.Verification;

namespace RiktigRefactor.CSharp.Tests;

public class RenameCaseTests
{
    /// <summary>Set to 1 to write the produced output into each case's expected directory. Review the diff before committing.</summary>
    const string WriteExpectedVariable = "RIKTIG_WRITE_EXPECTED";

    public static TheoryData<string> Cases => [.. TestCase.FindNames("Rename")];

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Case(string name)
    {
        var testCase = TestCase.Load(name);
        var (inputFiles, selection) = SelectionMarkers.Strip(testCase.InputFiles);
        var newName = testCase.Definition.Options?["newName"] ?? throw new InvalidOperationException($"{name}: options.newName is required.");

        Assert.Null(ProgramRunner.CompileAndRun(inputFiles));

        var solution = TestWorkspace.CreateSolution(inputFiles);
        var documentId = TestWorkspace.FindDocument(solution, selection.File);
        var result = await RenameRefactoring.RenameAsync(solution, documentId, selection.Start, newName, TestContext.Current.CancellationToken);

        if (testCase.Definition.Refuse is { } expectedCode)
        {
            Assert.Equal(RefactoringStatus.Refused, result.Status);
            Assert.Equal(expectedCode, Assert.Single(result.Reasons).Code);
            await AssertNaiveFailsIfRequiredAsync(testCase, solution, documentId, selection, newName);
            return;
        }

        Assert.True(result.Status == RefactoringStatus.Success, string.Join("\n", result.Reasons));
        var outputFiles = await TestWorkspace.ReadFilesAsync(result.NewSolution!);
        if (Environment.GetEnvironmentVariable(WriteExpectedVariable) == "1")
        {
            WriteExpected(name, outputFiles);
        }

        var expectedFiles = testCase.ExpectedFiles ?? throw new InvalidOperationException($"{name}: expected directory is missing.");
        Assert.Equal(expectedFiles.Keys.Order(StringComparer.Ordinal), outputFiles.Keys.Order(StringComparer.Ordinal));
        foreach (var (file, expectedText) in expectedFiles)
        {
            Assert.Equal(expectedText, outputFiles[file]);
        }

        Assert.Null(ProgramRunner.CompileAndRun(outputFiles));
        Assert.Equal(outputFiles.Where(file => file.Value != inputFiles[file.Key]).Select(file => file.Key).Order(StringComparer.Ordinal), result.Edits.Select(edit => edit.File).Distinct().Order(StringComparer.Ordinal));
        foreach (var (file, text) in outputFiles)
        {
            Assert.Equal(text, ApplyEdits(inputFiles[file], result.Edits.Where(edit => edit.File == file)));
        }

        await AssertNaiveFailsIfRequiredAsync(testCase, solution, documentId, selection, newName);
    }

    static async Task AssertNaiveFailsIfRequiredAsync(TestCase testCase, Solution solution, DocumentId documentId, Selection selection, string newName)
    {
        if (!testCase.Definition.NaiveShouldFail)
        {
            return;
        }

        var naiveSolution = await NaiveRename.RenameAsync(solution, documentId, selection.Start, newName);
        var naiveFailure = ProgramRunner.CompileAndRun(await TestWorkspace.ReadFilesAsync(naiveSolution));
        Assert.True(naiveFailure is not null, "The naive rename passed this test, so the test can't detect the edge case it targets.");
    }

    static string ApplyEdits(string text, IEnumerable<FileEdit> edits)
    {
        var sourceText = SourceText.From(text);
        var changes = edits.Select(edit => new TextChange(
            TextSpan.FromBounds(Ranges.ToOffset(sourceText, edit.Start), Ranges.ToOffset(sourceText, edit.End)),
            edit.NewText));
        return sourceText.WithChanges(changes).ToString();
    }

    static void WriteExpected(string name, IReadOnlyDictionary<string, string> outputFiles)
    {
        var directory = TestCase.ExpectedDirectory(name);
        Directory.CreateDirectory(directory);
        foreach (var (file, text) in outputFiles)
        {
            File.WriteAllText(Path.Combine(directory, file), text);
        }
    }
}
