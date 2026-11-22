using Microsoft.CodeAnalysis;

namespace RiktigRefactor.CSharp.Verification;

/// <summary>
/// Describes a symbol as a string that identifies the same declaration within one solution.
/// </summary>
public static class SymbolIdentity
{
    public static ISymbol Normalize(ISymbol symbol)
    {
        var definition = symbol.OriginalDefinition;
        return definition is IMethodSymbol { ReducedFrom: { } unreduced } ? unreduced.OriginalDefinition : definition;
    }

    public static string Describe(Solution solution, ISymbol symbol)
    {
        var definition = Normalize(symbol);
        if (definition is INamespaceSymbol { IsGlobalNamespace: true })
        {
            return "global-namespace";
        }

        var sourceLocation = PreferredSourceLocation(solution, definition);
        var documentId = sourceLocation?.SourceTree is { } tree ? solution.GetDocumentId(tree) : null;
        if (sourceLocation is not null && documentId is not null)
        {
            return SourceId(definition.Kind, documentId, sourceLocation.SourceSpan.Start);
        }

        return $"metadata:{definition.Kind}:{definition.ContainingAssembly?.Identity.GetDisplayName()}:{definition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}:{definition.GetDocumentationCommentId()}";
    }

    /// <summary>
    /// The first location in a regular document, or else the first in a source-generated one.
    /// A partial type can be declared in both, and only regular documents can be mapped across the change.
    /// </summary>
    public static Location? PreferredSourceLocation(Solution solution, ISymbol definition)
    {
        var sourceLocations = definition.Locations.Where(location => location.IsInSource).ToList();
        return sourceLocations.FirstOrDefault(location => RegularDocumentId(solution, location.SourceTree) is not null)
            ?? sourceLocations.FirstOrDefault();
    }

    /// <summary>The document containing <paramref name="tree"/>, unless it is missing or source-generated.</summary>
    public static DocumentId? RegularDocumentId(Solution solution, SyntaxTree? tree)
        => tree is not null && solution.GetDocument(tree) is { } document and not SourceGeneratedDocument ? document.Id : null;

    public static string SourceId(SymbolKind kind, DocumentId documentId, int start)
        => $"source:{kind}:{documentId.Id}:{start}";
}
