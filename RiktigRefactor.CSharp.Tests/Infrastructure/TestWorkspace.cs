using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace RiktigRefactor.CSharp.Tests.Infrastructure;

public static class TestWorkspace
{
    public const string SingleProjectName = "TestProgram";

    public static Solution CreateSolution(IReadOnlyDictionary<string, string> files, IReadOnlyDictionary<string, IReadOnlyList<string>>? projects = null)
    {
        var workspace = new AdhocWorkspace();
        var solution = workspace.CurrentSolution;
        var projectIds = (projects?.Keys ?? [SingleProjectName]).ToDictionary(name => name, _ => ProjectId.CreateNewId(), StringComparer.Ordinal);
        var projectOrder = projects?.Keys.ToList() ?? [SingleProjectName];
        foreach (var name in projectOrder)
        {
            var isEntry = name == projectOrder[^1];
            solution = solution.AddProject(ProjectInfo.Create(
                projectIds[name],
                VersionStamp.Create(),
                name,
                name,
                LanguageNames.CSharp,
                compilationOptions: new CSharpCompilationOptions(isEntry ? OutputKind.ConsoleApplication : OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable),
                parseOptions: ProgramRunner.ParseOptions,
                metadataReferences: ProgramRunner.References,
                projectReferences: projects?[name].Select(reference => new ProjectReference(projectIds[reference]))));
        }

        foreach (var (name, text) in files.OrderBy(file => file.Key, StringComparer.Ordinal))
        {
            var project = projects is null ? SingleProjectName : name[..name.IndexOf('/')];
            solution = solution.AddDocument(DocumentId.CreateNewId(projectIds[project], name), name, SourceText.From(text), filePath: name);
        }

        return solution;
    }

    public static DocumentId FindDocument(Solution solution, string name)
        => solution.Projects.SelectMany(project => project.Documents).Single(document => document.Name == name).Id;

    public static async Task<Dictionary<string, string>> ReadFilesAsync(Solution solution)
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var document in solution.Projects.SelectMany(project => project.Documents))
        {
            files[document.Name] = (await document.GetTextAsync().ConfigureAwait(false)).ToString();
        }

        return files;
    }
}
