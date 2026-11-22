using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;

namespace RiktigRefactor.CSharp.Tests.Infrastructure;

/// <summary>
/// Renames the declaration and every reference found by Roslyn, with no conflict checks.
/// Edge-case tests must fail under this implementation, which proves they can detect a behavior change.
/// </summary>
public static class NaiveRename
{
    public static async Task<Solution> RenameAsync(Solution solution, DocumentId documentId, int position, string newName)
    {
        var document = solution.GetDocument(documentId)!;
        var symbol = await SymbolFinder.FindSymbolAtPositionAsync(document, position).ConfigureAwait(false)
            ?? throw new InvalidOperationException("No symbol at the cursor.");
        if (symbol is IMethodSymbol { MethodKind: MethodKind.Constructor })
        {
            symbol = symbol.ContainingType;
        }

        var locations = symbol.Locations.Where(location => location.IsInSource).ToList();
        foreach (var referenced in await SymbolFinder.FindReferencesAsync(symbol, solution).ConfigureAwait(false))
        {
            locations.AddRange(referenced.Definition.Locations.Where(location => location.IsInSource));
            locations.AddRange(referenced.Locations.Select(reference => reference.Location));
        }

        foreach (var group in locations.Distinct().GroupBy(location => solution.GetDocumentId(location.SourceTree)!))
        {
            var text = await solution.GetDocument(group.Key)!.GetTextAsync().ConfigureAwait(false);
            var changes = group.Select(location => new TextChange(location.SourceSpan, newName));
            solution = solution.WithDocumentText(group.Key, text.WithChanges(changes));
        }

        return solution;
    }
}
