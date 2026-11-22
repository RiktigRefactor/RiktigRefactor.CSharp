using System.Runtime.CompilerServices;
using System.Text.Json;

namespace RiktigRefactor.CSharp.Tests.Infrastructure;

/// <summary>
/// A test case directory: <c>case.json</c>, an <c>input</c> directory, and (unless a refusal is expected) an <c>expected</c> directory.
/// </summary>
public record TestCase(
    string Name,
    CaseDefinition Definition,
    IReadOnlyDictionary<string, string> InputFiles,
    IReadOnlyDictionary<string, string>? ExpectedFiles)
{
    static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string CasesDirectory([CallerFilePath] string thisFile = "")
        => Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(thisFile)!)!, "Cases");

    public static string ExpectedDirectory(string name) => Path.Combine(CasesDirectory(), name, "expected");

    public static IEnumerable<string> FindNames(string refactoring)
    {
        var root = Path.Combine(CasesDirectory(), refactoring);
        return Directory.EnumerateFiles(root, "case.json", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(CasesDirectory(), Path.GetDirectoryName(path)!).Replace('\\', '/'))
            .Order(StringComparer.Ordinal);
    }

    public static TestCase Load(string name)
    {
        var directory = Path.Combine(CasesDirectory(), name);
        var definition = JsonSerializer.Deserialize<CaseDefinition>(File.ReadAllText(Path.Combine(directory, "case.json")), JsonOptions)
            ?? throw new InvalidOperationException($"{name}: empty case.json");
        var expectedDirectory = ExpectedDirectory(name);
        return new TestCase(
            name,
            definition,
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
