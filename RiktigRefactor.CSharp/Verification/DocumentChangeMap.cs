using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RiktigRefactor.CSharp.Verification;

/// <summary>
/// Maps tokens of an original document to the corresponding tokens of the changed document by walking both syntax trees in parallel.
/// A rename changes identifier text, and may wrap a simple name in a qualification (such as <c>this.name</c>).
/// Any other change in shape is recorded in <see cref="UnexpectedChange"/>, and the affected tokens stay unmapped.
/// </summary>
public class DocumentChangeMap
{
    readonly Dictionary<int, SyntaxToken> _newTokensByOldStart = [];

    public DocumentChangeMap(SyntaxNode oldRoot, SyntaxNode newRoot)
    {
        Align(oldRoot, newRoot);
    }

    /// <summary>Gets the first part of the original document that was rewritten in a way other than renaming or qualifying a name.</summary>
    public SyntaxNodeOrToken? UnexpectedChange { get; private set; }

    /// <summary>
    /// Finds the token in the new document that corresponds to the token that started at <paramref name="oldStart"/>,
    /// expecting it to read <paramref name="expectedValueText"/>. Returns null if there is no such token.
    /// </summary>
    public SyntaxToken? FindNewToken(int oldStart, string expectedValueText)
        => this._newTokensByOldStart.TryGetValue(oldStart, out var token) && token.ValueText == expectedValueText ? token : null;

    static SimpleNameSyntax? RightmostName(SyntaxNode node) => node switch
    {
        MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
        QualifiedNameSyntax qualified => qualified.Right,
        AliasQualifiedNameSyntax aliasQualified => aliasQualified.Name,
        _ => null,
    };

    void Align(SyntaxNodeOrToken oldItem, SyntaxNodeOrToken newItem)
    {
        if (oldItem.IsToken && newItem.IsToken)
        {
            AlignTokens(oldItem.AsToken(), newItem.AsToken());
        }
        else if (oldItem.IsNode && newItem.IsNode)
        {
            AlignNodes(oldItem.AsNode()!, newItem.AsNode()!);
        }
        else
        {
            UnexpectedChange ??= oldItem;
        }
    }

    void AlignTokens(SyntaxToken oldToken, SyntaxToken newToken)
    {
        this._newTokensByOldStart[oldToken.SpanStart] = newToken;
        var isSameShape = oldToken.RawKind == newToken.RawKind
            && (oldToken.IsKind(SyntaxKind.IdentifierToken) || oldToken.Text == newToken.Text);
        if (!isSameShape)
        {
            UnexpectedChange ??= oldToken;
        }

        AlignTrivia(oldToken.LeadingTrivia, newToken.LeadingTrivia);
        AlignTrivia(oldToken.TrailingTrivia, newToken.TrailingTrivia);
    }

    void AlignTrivia(SyntaxTriviaList oldTrivia, SyntaxTriviaList newTrivia)
    {
        var oldStructured = oldTrivia.Where(trivia => trivia.HasStructure).ToList();
        var newStructured = newTrivia.Where(trivia => trivia.HasStructure).ToList();
        if (oldStructured.Count != newStructured.Count)
        {
            UnexpectedChange ??= oldStructured.FirstOrDefault().GetStructure();
            return;
        }

        for (var index = 0; index < oldStructured.Count; index++)
        {
            Align(oldStructured[index].GetStructure()!, newStructured[index].GetStructure()!);
        }
    }

    void AlignNodes(SyntaxNode oldNode, SyntaxNode newNode)
    {
        if (oldNode.RawKind != newNode.RawKind)
        {
            if (oldNode is SimpleNameSyntax && RightmostName(newNode) is { } name)
            {
                Align(oldNode, name);
            }
            else
            {
                UnexpectedChange ??= oldNode;
            }

            return;
        }

        var oldChildren = oldNode.ChildNodesAndTokens();
        var newChildren = newNode.ChildNodesAndTokens();
        if (oldChildren.Count != newChildren.Count)
        {
            UnexpectedChange ??= oldNode;
            return;
        }

        for (var index = 0; index < oldChildren.Count; index++)
        {
            Align(oldChildren[index], newChildren[index]);
        }
    }
}
