using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;

namespace RiktigRefactor.CSharp.Rename;

public static class SymbolsAcrossChanges
{
    /// <summary>Finds <paramref name="symbol"/> from <paramref name="oldSolution"/> again in <paramref name="newSolution"/>, after edits that kept its declaration.</summary>
    public static async Task<ISymbol?> FindSameSymbolAsync(Solution oldSolution, Solution newSolution, ISymbol symbol, CancellationToken cancellationToken)
    {
        var declaringTree = symbol.Locations.First(location => location.IsInSource).SourceTree;
        var projectId = oldSolution.GetDocumentId(declaringTree)!.ProjectId;
        var compilation = await newSolution.GetProject(projectId)!.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        return SymbolFinder.FindSimilarSymbols(symbol, compilation!, cancellationToken).FirstOrDefault();
    }
}
