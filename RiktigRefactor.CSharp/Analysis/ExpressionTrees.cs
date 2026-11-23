using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RiktigRefactor.CSharp.Analysis;

public static class ExpressionTrees
{
    /// <summary>True inside a query expression or a lambda converted to an expression tree, where rewriting code changes the tree that a provider sees.</summary>
    public static bool IsInsideExpressionTreeOrQuery(SemanticModel model, SyntaxNode node, CancellationToken cancellationToken)
        => node.Ancestors().Any(ancestor => ancestor is QueryExpressionSyntax
            || (ancestor is LambdaExpressionSyntax lambda && IsExpressionTree(model.GetTypeInfo(lambda, cancellationToken).ConvertedType)));

    static bool IsExpressionTree(ITypeSymbol? type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.ToDisplayString() == "System.Linq.Expressions.LambdaExpression")
            {
                return true;
            }
        }

        return false;
    }
}
