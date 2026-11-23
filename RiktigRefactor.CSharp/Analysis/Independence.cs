using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RiktigRefactor.CSharp.Analysis;

/// <summary>
/// Proves two expressions independent, as the specification defines it: evaluating them in either order gives the same behavior.
/// Only simple cases are proven: a constant, <c>this</c>, or a read of a local or parameter that the other expression can't write.
/// </summary>
public static class Independence
{
    public static bool AreIndependent(SemanticModel model, ExpressionSyntax first, ExpressionSyntax second, CancellationToken cancellationToken)
        => IsIndependentOfAnything(model, first, cancellationToken)
            || IsIndependentOfAnything(model, second, cancellationToken)
            || IsUnwrittenVariableRead(model, first, second, cancellationToken)
            || IsUnwrittenVariableRead(model, second, first, cancellationToken);

    static ExpressionSyntax Unparenthesized(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            expression = parenthesized.Expression;
        }

        return expression;
    }

    static bool IsIndependentOfAnything(SemanticModel model, ExpressionSyntax expression, CancellationToken cancellationToken)
    {
        var inner = Unparenthesized(expression);
        return inner is LiteralExpressionSyntax or ThisExpressionSyntax
            || model.GetConstantValue(inner, cancellationToken).HasValue;
    }

    /// <summary>True if <paramref name="read"/> reads a local or parameter that <paramref name="other"/> can't write: it names it in no written position, and no lambda or local function captures it.</summary>
    static bool IsUnwrittenVariableRead(SemanticModel model, ExpressionSyntax read, ExpressionSyntax other, CancellationToken cancellationToken)
    {
        if (Unparenthesized(read) is not IdentifierNameSyntax name
            || model.GetSymbolInfo(name, cancellationToken).Symbol is not { } variable
            || variable is not (ILocalSymbol { IsRef: false } or IParameterSymbol { RefKind: RefKind.None }))
        {
            return false;
        }

        var isWrittenByOther = other.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
            .Any(candidate => candidate.Identifier.ValueText == variable.Name
                && IsWrittenPosition(candidate)
                && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(candidate, cancellationToken).Symbol, variable));
        return !isWrittenByOther && !IsCaptured(model, name, variable, cancellationToken);
    }

    static bool IsCaptured(SemanticModel model, SyntaxNode node, ISymbol variable, CancellationToken cancellationToken)
    {
        var scope = node.FirstAncestorOrSelf<MemberDeclarationSyntax>() ?? node.SyntaxTree.GetRoot(cancellationToken);
        return scope.DescendantNodes()
            .Where(candidate => candidate is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax)
            .Any(function => model.AnalyzeDataFlow(function) is { Succeeded: true } flow && flow.Captured.Contains(variable, SymbolEqualityComparer.Default));
    }

    public static bool IsWrittenPosition(ExpressionSyntax name)
    {
        SyntaxNode node = name;
        while (node.Parent is ParenthesizedExpressionSyntax or ArgumentSyntax { Parent: TupleExpressionSyntax } or TupleExpressionSyntax)
        {
            node = node.Parent;
        }

        return node.Parent switch
        {
            AssignmentExpressionSyntax assignment => assignment.Left == node,
            PrefixUnaryExpressionSyntax prefix => prefix.IsKind(SyntaxKind.PreIncrementExpression) || prefix.IsKind(SyntaxKind.PreDecrementExpression) || prefix.IsKind(SyntaxKind.AddressOfExpression),
            PostfixUnaryExpressionSyntax => true,
            ArgumentSyntax argument => !argument.RefKindKeyword.IsKind(SyntaxKind.None),
            RefExpressionSyntax => true,
            _ => false,
        };
    }
}
