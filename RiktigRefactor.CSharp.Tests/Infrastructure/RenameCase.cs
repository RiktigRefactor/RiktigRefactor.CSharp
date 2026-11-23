using Microsoft.CodeAnalysis;
using RiktigRefactor.CSharp.Protocol;
using RiktigRefactor.CSharp.Rename;

namespace RiktigRefactor.CSharp.Tests.Infrastructure;

/// <summary>
/// A rename test whose input (and, unless a refusal is expected, expected output) live as files in a directory under <c>Cases/Rename</c>.
/// </summary>
/// <param name="CaseDirectory">The case directory relative to <c>Cases/Rename</c>, such as <c>basic/local-variable</c>.</param>
/// <param name="NewName">The name to rename to.</param>
/// <param name="Projects">For multi-project cases: project name to the names of projects it references, in build order. The last project is the program entry point. A file belongs to the project named by its first path segment. Null means a single project.</param>
/// <param name="RefusalCode">The expected refusal code, or null if the rename should succeed.</param>
/// <param name="NaiveMustFail">True for edge cases: the naive rename must produce a program that fails.</param>
/// <param name="NaiveFailureContains">When set, the naive rename's failure message must contain this text, so the case proves it fails for the reason it names.</param>
public record RenameCase(
    string CaseDirectory,
    string? NewName = null,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? Projects = null,
    string? RefusalCode = null,
    bool NaiveMustFail = false,
    string? NaiveFailureContains = null)
{
    public static RenameCase InDirectory(string caseDirectory) => new(caseDirectory);

    public RenameCase RenameTo(string newName) => this with { NewName = newName };

    public RenameCase WithProjects(params (string Name, string[] References)[] projects)
        => this with { Projects = projects.ToDictionary(project => project.Name, project => (IReadOnlyList<string>)project.References, StringComparer.Ordinal) };

    public RenameCase ExpectRefusal(string code) => this with { RefusalCode = code };

    public RenameCase NaiveShouldFail() => this with { NaiveMustFail = true };

    public RenameCase NaiveShouldFailWith(string failureText) => this with { NaiveMustFail = true, NaiveFailureContains = failureText };

    public async Task RunAsync()
    {
        var name = "Rename/" + CaseDirectory;
        var testCase = TestCase.Load(name);
        var (inputFiles, selection) = SelectionMarkers.Strip(testCase.InputFiles);
        var newName = NewName ?? throw new InvalidOperationException($"{name}: call RenameTo.");

        Assert.Null(ProgramRunner.CompileAndRun(inputFiles, Projects));

        var solution = TestWorkspace.CreateSolution(inputFiles, Projects);
        var documentId = TestWorkspace.FindDocument(solution, selection.File);
        var result = await RenameRefactoring.RenameAsync(solution, documentId, selection.Start, newName, TestContext.Current.CancellationToken);

        if (RefusalCode is { } expectedCode)
        {
            Assert.Equal(RefactoringStatus.Refused, result.Status);
            Assert.Equal(expectedCode, Assert.Single(result.Reasons).Code);
            await AssertNaiveFailsIfRequiredAsync(solution, documentId, selection, newName);
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

        Assert.Null(ProgramRunner.CompileAndRun(outputFiles, Projects));
        Assert.Equal(outputFiles.Where(file => file.Value != inputFiles[file.Key]).Select(file => file.Key).Order(StringComparer.Ordinal), result.Edits.Select(edit => edit.File).Distinct().Order(StringComparer.Ordinal));
        foreach (var (file, text) in outputFiles)
        {
            Assert.Equal(text, CaseFiles.ApplyEdits(inputFiles[file], result.Edits.Where(edit => edit.File == file)));
        }

        await AssertNaiveFailsIfRequiredAsync(solution, documentId, selection, newName);
    }

    async Task AssertNaiveFailsIfRequiredAsync(Solution solution, DocumentId documentId, Selection selection, string newName)
    {
        if (!NaiveMustFail)
        {
            return;
        }

        var naiveSolution = await NaiveRename.RenameAsync(solution, documentId, selection.Start, newName);
        var naiveFailure = ProgramRunner.CompileAndRun(await TestWorkspace.ReadFilesAsync(naiveSolution), Projects);
        Assert.True(naiveFailure is not null, "The naive rename passed this test, so the test can't detect the edge case it targets.");
        if (NaiveFailureContains is not null)
        {
            Assert.Contains(NaiveFailureContains, naiveFailure);
        }
    }
}
