using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RiktigRefactor.CSharp.ExtractIife;
using RiktigRefactor.CSharp.Protocol;

namespace RiktigRefactor.CSharp.Tests.Infrastructure;

/// <summary>
/// An Extract IIFE test whose input (and, unless a refusal is expected, expected output) live as files in a directory under <c>Cases/ExtractIife</c>.
/// The input marks the selection with <c>/*[*/</c> and <c>/*]*/</c>.
/// </summary>
/// <param name="CaseDirectory">The case directory relative to <c>Cases/ExtractIife</c>, such as <c>expression/arithmetic</c>.</param>
/// <param name="RefusalCode">The expected refusal code, or null if the extraction should succeed.</param>
/// <param name="NaiveMustFail">True for edge cases: the naive extraction must produce a program that fails.</param>
public record ExtractIifeCase(string CaseDirectory, string? RefusalCode = null, bool NaiveMustFail = false)
{
    public static ExtractIifeCase InDirectory(string caseDirectory) => new(caseDirectory);

    public ExtractIifeCase ExpectRefusal(string code) => this with { RefusalCode = code };

    public ExtractIifeCase NaiveShouldFail() => this with { NaiveMustFail = true };

    public async Task RunAsync()
    {
        var name = "ExtractIife/" + CaseDirectory;
        var testCase = TestCase.Load(name);
        var (inputFiles, selection) = SelectionMarkers.Strip(testCase.InputFiles);
        var span = TextSpan.FromBounds(selection.Start, selection.End);

        Assert.Null(ProgramRunner.CompileAndRun(inputFiles));

        var solution = TestWorkspace.CreateSolution(inputFiles);
        var documentId = TestWorkspace.FindDocument(solution, selection.File);
        var result = await ExtractIifeRefactoring.ExtractAsync(solution, documentId, span, TestContext.Current.CancellationToken);

        if (RefusalCode is { } expectedCode)
        {
            Assert.True(result.Status == RefactoringStatus.Refused, "Expected a refusal, but the extraction succeeded.");
            Assert.Equal(expectedCode, Assert.Single(result.Reasons).Code);
            await AssertNaiveFailsIfRequiredAsync(solution, documentId, span);
            return;
        }

        Assert.True(result.Status == RefactoringStatus.Success, string.Join("\n", result.Reasons));
        var outputFiles = await TestWorkspace.ReadFilesAsync(result.NewSolution!);
        CaseFiles.WriteExpectedIfRequested(name, outputFiles);

        var expectedFiles = testCase.ExpectedFiles ?? throw new InvalidOperationException($"{name}: expected directory is missing.");
        Assert.Equal(expectedFiles.Keys.Order(StringComparer.Ordinal), outputFiles.Keys.Order(StringComparer.Ordinal));
        foreach (var (file, expectedText) in expectedFiles)
        {
            Assert.Equal(expectedText, outputFiles[file]);
        }

        Assert.Null(ProgramRunner.CompileAndRun(outputFiles));
        foreach (var (file, text) in outputFiles)
        {
            Assert.Equal(text, CaseFiles.ApplyEdits(inputFiles[file], result.Edits.Where(edit => edit.File == file)));
        }

        await AssertNaiveFailsIfRequiredAsync(solution, documentId, span);
    }

    async Task AssertNaiveFailsIfRequiredAsync(Solution solution, DocumentId documentId, TextSpan span)
    {
        if (!NaiveMustFail)
        {
            return;
        }

        var naiveSolution = await NaiveExtractIife.ExtractAsync(solution, documentId, span);
        var naiveFailure = ProgramRunner.CompileAndRun(await TestWorkspace.ReadFilesAsync(naiveSolution));
        Assert.True(naiveFailure is not null, "The naive extraction passed this test, so the test can't detect the edge case it targets.");
    }
}
