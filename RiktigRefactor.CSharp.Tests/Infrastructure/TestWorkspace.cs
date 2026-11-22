using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace RiktigRefactor.CSharp.Tests.Infrastructure;

public static class TestWorkspace
{
    public static Solution CreateSolution(IReadOnlyDictionary<string, string> files)
    {
        var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var solution = workspace.CurrentSolution
            .AddProject(ProjectInfo.Create(
                projectId,
                VersionStamp.Create(),
                "TestProgram",
                "TestProgram",
                LanguageNames.CSharp,
                compilationOptions: new CSharpCompilationOptions(OutputKind.ConsoleApplication, nullableContextOptions: NullableContextOptions.Enable),
                parseOptions: ProgramRunner.ParseOptions,
                metadataReferences: ProgramRunner.References));

        foreach (var (name, text) in files.OrderBy(file => file.Key, StringComparer.Ordinal))
        {
            solution = solution.AddDocument(DocumentId.CreateNewId(projectId, name), name, SourceText.From(text), filePath: name);
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
