using Microsoft.CodeAnalysis;

namespace RiktigRefactor.CSharp.Verification;

/// <summary>Finds compile errors that a change adds.</summary>
public static class CompileErrors
{
    /// <returns>The first new error of each kind and file, or an empty list.</returns>
    public static async Task<List<Diagnostic>> FindNewAsync(Solution oldSolution, Solution newSolution, CancellationToken cancellationToken)
    {
        var before = await CountAsync(oldSolution, cancellationToken).ConfigureAwait(false);
        var after = await CountAsync(newSolution, cancellationToken).ConfigureAwait(false);
        return [.. after.Where(pair => pair.Value.Count > (before.TryGetValue(pair.Key, out var old) ? old.Count : 0)).Select(pair => pair.Value.First)];
    }

    static async Task<Dictionary<(string File, string Id), (int Count, Diagnostic First)>> CountAsync(Solution solution, CancellationToken cancellationToken)
    {
        var counts = new Dictionary<(string File, string Id), (int Count, Diagnostic First)>();
        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            foreach (var error in compilation?.GetDiagnostics(cancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error) ?? [])
            {
                var key = (error.Location.SourceTree?.FilePath ?? project.Name, error.Id);
                var existing = counts.TryGetValue(key, out var found) ? found : (Count: 0, First: error);
                counts[key] = (existing.Count + 1, existing.First);
            }
        }

        return counts;
    }
}
