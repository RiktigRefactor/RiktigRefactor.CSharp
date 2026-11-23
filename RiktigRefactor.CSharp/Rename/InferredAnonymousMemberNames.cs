using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace RiktigRefactor.CSharp.Rename;

/// <summary>
/// An anonymous type member written as <c>new { box.Count }</c> takes its name from the last name in the expression,
/// so renaming <c>Count</c> would rename the member too, changing the type's ToString output and equality.
/// Writing the inferred name explicitly (<c>new { Count = box.Count }</c>) declares the same member, and keeps it when <c>Count</c> is renamed.
/// </summary>
public static class InferredAnonymousMemberNames
{
    /// <summary>
    /// Gives an explicit name to every anonymous type member whose name is inferred from a reference to <paramref name="symbol"/>.
    /// Returns null if the anonymous types didn't keep exactly the same member names, which would mean the rewrite is wrong.
    /// </summary>
    public static async Task<(Solution Solution, ISymbol Symbol)?> MakeExplicitAsync(Solution solution, ISymbol symbol, CancellationToken cancellationToken)
    {
        var declaratorsByDocument = await FindDeclaratorsNamedBySymbolAsync(solution, symbol, cancellationToken).ConfigureAwait(false);
        if (declaratorsByDocument.Count == 0)
        {
            return (solution, symbol);
        }

        var explicitSolution = solution;
        foreach (var (documentId, declarators) in declaratorsByDocument)
        {
            var root = await solution.GetDocument(documentId)!.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root!.ReplaceNodes(declarators, (original, _) => WithExplicitName(original));
            explicitSolution = explicitSolution.WithDocumentSyntaxRoot(documentId, newRoot);
        }

        var keptMemberNames = await HaveSameAnonymousMemberNamesAsync(solution, explicitSolution, declaratorsByDocument.Keys, cancellationToken).ConfigureAwait(false);
        var explicitSymbol = await SymbolsAcrossChanges.FindSameSymbolAsync(solution, explicitSolution, symbol, cancellationToken).ConfigureAwait(false);
        if (!keptMemberNames || explicitSymbol is null)
        {
            return null;
        }

        return (explicitSolution, explicitSymbol);
    }

    static async Task<Dictionary<DocumentId, List<AnonymousObjectMemberDeclaratorSyntax>>> FindDeclaratorsNamedBySymbolAsync(
        Solution solution,
        ISymbol symbol,
        CancellationToken cancellationToken)
    {
        var declaratorsByDocument = new Dictionary<DocumentId, List<AnonymousObjectMemberDeclaratorSyntax>>();
        var references = await SymbolFinder.FindReferencesAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
        foreach (var location in references.SelectMany(reference => reference.Locations))
        {
            var root = await location.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var name = root!.FindNode(location.Location.SourceSpan, getInnermostNodeForTie: true);
            if (ExpressionWhoseLastNameIs(name).Parent is AnonymousObjectMemberDeclaratorSyntax { NameEquals: null } declarator)
            {
                var declarators = declaratorsByDocument.TryGetValue(location.Document.Id, out var existing) ? existing : [];
                declarators.Add(declarator);
                declaratorsByDocument[location.Document.Id] = declarators;
            }
        }

        return declaratorsByDocument;
    }

    static AnonymousObjectMemberDeclaratorSyntax WithExplicitName(AnonymousObjectMemberDeclaratorSyntax declarator)
    {
        var inferredName = SyntaxFacts.TryGetInferredMemberName(declarator.Expression)!;
        var nameEquals = SyntaxFactory.NameEquals(
                SyntaxFactory.IdentifierName(inferredName),
                SyntaxFactory.Token(SyntaxKind.EqualsToken).WithLeadingTrivia(SyntaxFactory.Space).WithTrailingTrivia(SyntaxFactory.Space))
            .WithLeadingTrivia(declarator.Expression.GetLeadingTrivia());
        return declarator.WithNameEquals(nameEquals).WithExpression(declarator.Expression.WithoutLeadingTrivia());
    }

    static SyntaxNode ExpressionWhoseLastNameIs(SyntaxNode name)
    {
        var expression = name;
        while (EndsWith(expression.Parent, expression))
        {
            expression = expression.Parent!;
        }

        return expression;
    }

    static bool EndsWith(SyntaxNode? expression, SyntaxNode lastPart) => expression switch
    {
        MemberAccessExpressionSyntax access => access.Name == lastPart,
        MemberBindingExpressionSyntax => true,
        ConditionalAccessExpressionSyntax conditional => conditional.WhenNotNull == lastPart,
        _ => false,
    };

    static async Task<bool> HaveSameAnonymousMemberNamesAsync(
        Solution oldSolution,
        Solution newSolution,
        IEnumerable<DocumentId> documentIds,
        CancellationToken cancellationToken)
    {
        foreach (var documentId in documentIds)
        {
            var oldNames = await AnonymousMemberNamesAsync(oldSolution.GetDocument(documentId)!, cancellationToken).ConfigureAwait(false);
            var newNames = await AnonymousMemberNamesAsync(newSolution.GetDocument(documentId)!, cancellationToken).ConfigureAwait(false);
            if (!oldNames.SequenceEqual(newNames))
            {
                return false;
            }
        }

        return true;
    }

    static async Task<List<string>> AnonymousMemberNamesAsync(Document document, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        return
        [
            .. root!.DescendantNodes()
                .OfType<AnonymousObjectMemberDeclaratorSyntax>()
                .Select(declarator => model!.GetDeclaredSymbol(declarator, cancellationToken)?.Name ?? string.Empty),
        ];
    }
}
