using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RiktigRefactor.CSharp.Analysis;

/// <summary>
/// Proves that an expression can't be null at runtime, without trusting nullable annotations, which the runtime does not enforce.
/// </summary>
public static class NonNullExpressions
{
    public static bool IsProvablyNonNull(SemanticModel model, ExpressionSyntax expression, CancellationToken cancellationToken)
    {
        var inner = expression;
        while (inner is ParenthesizedExpressionSyntax parenthesized)
        {
            inner = parenthesized.Expression;
        }

        if (model.GetTypeInfo(inner, cancellationToken).Type is { IsValueType: true } type && type.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
        {
            return true;
        }

        return inner switch
        {
            ThisExpressionSyntax or BaseExpressionSyntax => true,
            BaseObjectCreationExpressionSyntax or ArrayCreationExpressionSyntax or ImplicitArrayCreationExpressionSyntax => true,
            InterpolatedStringExpressionSyntax => true,
            LiteralExpressionSyntax literal => literal.IsKind(SyntaxKind.StringLiteralExpression) || literal.IsKind(SyntaxKind.Utf8StringLiteralExpression),
            IdentifierNameSyntax name => IsLocalOnlyEverAssignedNonNull(model, name, cancellationToken),
            _ => false,
        };
    }

    /// <summary>A local declared with a non-null initializer and never written anywhere else.</summary>
    static bool IsLocalOnlyEverAssignedNonNull(SemanticModel model, IdentifierNameSyntax name, CancellationToken cancellationToken)
    {
        if (model.GetSymbolInfo(name, cancellationToken).Symbol is not ILocalSymbol { IsRef: false, IsConst: false } local
            || local.DeclaringSyntaxReferences is not [var reference]
            || reference.GetSyntax(cancellationToken) is not VariableDeclaratorSyntax { Initializer.Value: { } initializer } declarator
            || declarator.SyntaxTree != name.SyntaxTree)
        {
            return false;
        }

        if (declarator.Parent?.Parent is LocalDeclarationStatementSyntax { UsingKeyword.RawKind: not 0 } or not LocalDeclarationStatementSyntax)
        {
            return false;
        }

        var scope = (SyntaxNode?)declarator.FirstAncestorOrSelf<MemberDeclarationSyntax>() ?? declarator.SyntaxTree.GetRoot(cancellationToken);
        var isWrittenElsewhere = scope.DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Where(other => other.Identifier.ValueText == local.Name)
            .Any(other => IsPossiblyWritten(other) && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(other, cancellationToken).Symbol, local));
        return !isWrittenElsewhere && !initializer.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Any(other => other.Identifier.ValueText == local.Name)
            && IsProvablyNonNull(model, initializer, cancellationToken);
    }

    static bool IsPossiblyWritten(ExpressionSyntax name)
    {
        SyntaxNode node = name;
        while (node.Parent is ParenthesizedExpressionSyntax or ArgumentSyntax { Parent: TupleExpressionSyntax } or TupleExpressionSyntax)
        {
            node = node.Parent;
        }

        return node.Parent switch
        {
            AssignmentExpressionSyntax assignment => assignment.Left == node,
            PrefixUnaryExpressionSyntax or PostfixUnaryExpressionSyntax => true,
            ArgumentSyntax argument => !argument.RefKindKeyword.IsKind(SyntaxKind.None),
            RefExpressionSyntax => true,
            _ => false,
        };
    }
}
