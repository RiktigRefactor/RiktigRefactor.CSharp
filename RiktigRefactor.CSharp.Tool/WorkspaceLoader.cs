using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace RiktigRefactor.CSharp.Tool;

public static class WorkspaceLoader
{
    /// <summary>Loads a solution or project, throwing <see cref="WorkspaceLoadException"/> if anything fails to load.</summary>
    public static async Task<Solution> LoadAsync(string path, CancellationToken cancellationToken)
    {
        var failures = new List<string>();
        using var workspace = MSBuildWorkspace.Create();
        workspace.RegisterWorkspaceFailedHandler(args =>
        {
            if (args.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
            {
                failures.Add(args.Diagnostic.Message);
            }
        });

        var extension = Path.GetExtension(path).ToLowerInvariant();
        var solution = extension switch
        {
            ".sln" or ".slnx" or ".slnf" => await workspace.OpenSolutionAsync(path, cancellationToken: cancellationToken).ConfigureAwait(false),
            ".csproj" => (await workspace.OpenProjectAsync(path, cancellationToken: cancellationToken).ConfigureAwait(false)).Solution,
            _ => throw new WorkspaceLoadException($"Unsupported workspace `{path}`. Use a .sln, .slnx or .csproj."),
        };

        if (failures.Count > 0)
        {
            throw new WorkspaceLoadException($"Failed to load `{path}`:\n{string.Join("\n", failures.Distinct())}");
        }

        return solution;
    }
}
