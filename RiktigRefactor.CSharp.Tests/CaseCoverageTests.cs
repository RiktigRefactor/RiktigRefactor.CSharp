using System.Runtime.CompilerServices;
using RiktigRefactor.CSharp.Tests.Infrastructure;

namespace RiktigRefactor.CSharp.Tests;

public class CaseCoverageTests
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

    [Fact]
    public void EveryExtractIifeCaseDirectoryHasATest()
    {
        var testSource = string.Concat(Directory.EnumerateFiles(TestsDirectory(), "*ExtractIifeCaseTests.cs").Select(File.ReadAllText));

        var untested = TestCase.FindDirectories("ExtractIife")
            .Where(directory => !testSource.Contains($"InDirectory(\"{directory}\")", StringComparison.Ordinal))
            .ToList();

        Assert.Empty(untested);
    }

    [Theory]
    [InlineData("MakeMethodStatic")]
    public void EveryCaseDirectoryHasATest(string refactoring)
    {
        var testSource = string.Concat(Directory.EnumerateFiles(TestsDirectory(), $"*{refactoring}CaseTests.cs").Select(File.ReadAllText));

        var untested = TestCase.FindDirectories(refactoring)
            .Where(directory => !testSource.Contains($"InDirectory(\"{directory}\")", StringComparison.Ordinal))
            .ToList();

        Assert.Empty(untested);
    }

    static string TestsDirectory([CallerFilePath] string thisFile = "") => Path.GetDirectoryName(thisFile)!;
}
