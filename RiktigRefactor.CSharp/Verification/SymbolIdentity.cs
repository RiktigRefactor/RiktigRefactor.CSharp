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

        var sourceLocation = definition.Locations.FirstOrDefault(location => location.IsInSource);
        var documentId = sourceLocation?.SourceTree is { } tree ? solution.GetDocumentId(tree) : null;
        if (sourceLocation is not null && documentId is not null)
        {
            return SourceId(definition.Kind, documentId, sourceLocation.SourceSpan.Start);
        }

        return $"metadata:{definition.Kind}:{definition.ContainingAssembly?.Identity.GetDisplayName()}:{definition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}:{definition.GetDocumentationCommentId()}";
    }

    public static string SourceId(SymbolKind kind, DocumentId documentId, int start)
        => $"source:{kind}:{documentId.Id}:{start}";
}
