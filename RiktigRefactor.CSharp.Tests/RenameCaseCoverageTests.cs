using System.Runtime.CompilerServices;
using RiktigRefactor.CSharp.Tests.Infrastructure;

namespace RiktigRefactor.CSharp.Tests;

public class RenameCaseCoverageTests
{
    [Fact]
    public void EveryRenameCaseDirectoryHasATest()
    {
        var testSource = string.Concat(Directory.EnumerateFiles(TestsDirectory(), "*RenameCaseTests.cs").Select(File.ReadAllText));

        var untested = TestCase.FindDirectories("Rename")
            .Where(directory => !testSource.Contains($"InDirectory(\"{directory}\")", StringComparison.Ordinal))
            .ToList();

        Assert.Empty(untested);
    }

    static string TestsDirectory([CallerFilePath] string thisFile = "") => Path.GetDirectoryName(thisFile)!;
}
