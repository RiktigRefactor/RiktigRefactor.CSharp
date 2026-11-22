using RiktigRefactor.CSharp.Protocol;
using RiktigRefactor.CSharp.Rename;
using RiktigRefactor.CSharp.Tests.Infrastructure;

namespace RiktigRefactor.CSharp.Tests;

public class RenameWarningTests
{
    const string Source = """
        class Program
        {
            static void Main()
            {
                var /*|*/count = 1;
                var label = "count";
                System.Console.WriteLine(label + count);
            }
        }
        """;

    [Fact]
    public async Task WarnsWhenAStringEqualsTheOldName()
    {
        var result = await RenameAsync("total");

        Assert.Equal(RefactoringStatus.Success, result.Status);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal(WarningCodes.StringMatchesOldName, warning.Code);
        Assert.Equal(new Position(6, 21), warning.Location!.Start);
    }

    [Fact]
    public async Task RenamingToTheSameNameChangesNothing()
    {
        var result = await RenameAsync("count");

        Assert.Equal(RefactoringStatus.Success, result.Status);
        Assert.Empty(result.Edits);
    }

    static async Task<RefactoringResult> RenameAsync(string newName)
    {
        var (files, selection) = SelectionMarkers.Strip(new Dictionary<string, string> { ["Program.cs"] = Source });
        var solution = TestWorkspace.CreateSolution(files);
        var documentId = TestWorkspace.FindDocument(solution, selection.File);
        return await RenameRefactoring.RenameAsync(solution, documentId, selection.Start, newName, TestContext.Current.CancellationToken);
    }
}
