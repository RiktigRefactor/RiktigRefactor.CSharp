using System.Runtime.CompilerServices;

namespace RiktigRefactor.CSharp.Tests.Infrastructure;

/// <summary>
/// A test case directory: an <c>input</c> directory and, unless a refusal is expected, an <c>expected</c> directory.
/// </summary>
public record TestCase(
    string Name,
    IReadOnlyDictionary<string, string> InputFiles,
    IReadOnlyDictionary<string, string>? ExpectedFiles)
{
    public static string CasesDirectory([CallerFilePath] string thisFile = "")
        => Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(thisFile)!)!, "Cases");

    public static string ExpectedDirectory(string name) => Path.Combine(CasesDirectory(), name, "expected");

    public static IEnumerable<string> FindDirectories(string refactoring)
    {
        var root = Path.Combine(CasesDirectory(), refactoring);
        return Directory.EnumerateDirectories(root, "input", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, Path.GetDirectoryName(path)!).Replace('\\', '/'))
            .Order(StringComparer.Ordinal);
    }

    public static TestCase Load(string name)
    {
        var directory = Path.Combine(CasesDirectory(), name);
        var expectedDirectory = ExpectedDirectory(name);
        return new TestCase(
            name,
            ReadFiles(Path.Combine(directory, "input")),
            Directory.Exists(expectedDirectory) ? ReadFiles(expectedDirectory) : null);
    }

    static Dictionary<string, string> ReadFiles(string directory)
        => Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .ToDictionary(
                path => Path.GetRelativePath(directory, path).Replace('\\', '/'),
                File.ReadAllText,
                StringComparer.Ordinal);
}
