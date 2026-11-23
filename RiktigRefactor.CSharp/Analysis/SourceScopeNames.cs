using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RiktigRefactor.CSharp.Rewriting;

namespace RiktigRefactor.CSharp.Analysis;

/// <summary>Finds names in a declaration that are found through its type's scope, which a declaration moved to another type doesn't have, and qualifies them.</summary>
public static class SourceScopeNames
{
    /// <param name="skip">True for a symbol whose names are rewritten some other way.</param>
    public static List<SourceEdit> Qualify(SemanticModel model, DocumentId documentId, SyntaxNode declaration, INamedTypeSymbol sourceType, Func<ISymbol, bool> skip, CancellationToken cancellationToken)
    {
        var edits = new List<SourceEdit>();
        foreach (var name in declaration.DescendantNodes(descendIntoTrivia: true).OfType<SimpleNameSyntax>())
        {
            if (IsQualified(name) || model.GetSymbolInfo(name, cancellationToken).Symbol is not { } symbol || skip(symbol))
            {
                continue;
            }

            if ((symbol.IsStatic || symbol is INamedTypeSymbol) && symbol.ContainingType is { } containing && IsScopeOf(sourceType, containing))
            {
                edits.Add(SourceEdit.Insert(documentId, name.SpanStart, new TypePiece(containing.ToMinimalDisplayString(model, declaration.SpanStart), containing), new LiteralPiece(".")));
            }
        }

        return edits;
    }

    public static bool IsQualified(SimpleNameSyntax name) => name.Parent switch
    {
        MemberAccessExpressionSyntax memberAccess => memberAccess.Name == name,
        QualifiedNameSyntax qualified => qualified.Right == name,
        AliasQualifiedNameSyntax aliased => aliased.Name == name,
        MemberBindingExpressionSyntax => true,
        NameMemberCrefSyntax { Parent: QualifiedCrefSyntax } => true,
        NameColonSyntax or NameEqualsSyntax => true,
        _ => false,
    };

    /// <summary>True if an unqualified name in <paramref name="sourceType"/> can be found through <paramref name="type"/>: the type itself, its base types, its containing types and theirs.</summary>
    static bool IsScopeOf(INamedTypeSymbol sourceType, INamedTypeSymbol type)
    {
        var definition = type.OriginalDefinition;
        for (var outer = sourceType; outer is not null; outer = outer.ContainingType)
        {
            for (var current = outer; current is not null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, definition))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
