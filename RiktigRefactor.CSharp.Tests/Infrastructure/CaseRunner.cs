using Microsoft.CodeAnalysis;
using RiktigRefactor.CSharp.Protocol;

namespace RiktigRefactor.CSharp.Tests.Infrastructure;

/// <summary>Runs a case directory through a refactoring and its naive implementation, as the test format describes.</summary>
public static class CaseRunner
{
    /// <param name="name">The case directory relative to <c>Cases</c>, starting with the refactoring's directory.</param>
    /// <param name="refactor">Runs the refactoring on the marker-free input, given the document and selection the markers named.</param>
    /// <param name="naive">Runs the naive implementation in the same way.</param>
    public static async Task RunAsync(
        string name,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? projects,
        string? refusalCode,
        bool naiveMustFail,
        Func<Solution, DocumentId, Selection, Task<RefactoringResult>> refactor,
        Func<Solution, DocumentId, Selection, Task<Solution>> naive)
    {
        var testCase = TestCase.Load(name);
        var (inputFiles, selection) = SelectionMarkers.Strip(testCase.InputFiles);

        Assert.Null(ProgramRunner.CompileAndRun(inputFiles, projects));

        var solution = TestWorkspace.CreateSolution(inputFiles, projects);
        var documentId = TestWorkspace.FindDocument(solution, selection.File);
        var result = await refactor(solution, documentId, selection);

        if (refusalCode is { } expectedCode)
        {
            Assert.True(result.Status == RefactoringStatus.Refused, "Expected a refusal, but the refactoring succeeded.");
            Assert.Equal(expectedCode, Assert.Single(result.Reasons).Code);
        }
        else
        {
            Assert.True(result.Status == RefactoringStatus.Success, string.Join("\n", result.Reasons));
            var outputFiles = await TestWorkspace.ReadFilesAsync(result.NewSolution!);
            CaseFiles.WriteExpectedIfRequested(name, outputFiles);

            var expectedFiles = testCase.ExpectedFiles ?? throw new InvalidOperationException($"{name}: expected directory is missing.");
            Assert.Equal(expectedFiles.Keys.Order(StringComparer.Ordinal), outputFiles.Keys.Order(StringComparer.Ordinal));
            foreach (var (file, expectedText) in expectedFiles)
            {
                Assert.Equal(expectedText, outputFiles[file]);
            }

            Assert.Null(ProgramRunner.CompileAndRun(outputFiles, projects));
            foreach (var (file, text) in outputFiles)
            {
                Assert.Equal(text, CaseFiles.ApplyEdits(inputFiles[file], result.Edits.Where(edit => edit.File == file)));
            }
        }

        if (naiveMustFail)
        {
            var naiveSolution = await naive(solution, documentId, selection);
            var naiveFailure = ProgramRunner.CompileAndRun(await TestWorkspace.ReadFilesAsync(naiveSolution), projects);
            Assert.True(naiveFailure is not null, "The naive implementation passed this test, so the test can't detect the edge case it targets.");
        }
    }
}
