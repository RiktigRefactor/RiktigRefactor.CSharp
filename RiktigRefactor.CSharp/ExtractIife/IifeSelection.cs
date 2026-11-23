using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace RiktigRefactor.CSharp.ExtractIife;

/// <summary>
/// What the user selected: one expression, or a contiguous run of whole statements in one block.
/// </summary>
/// <param name="Span">The selection without surrounding whitespace and comments: from the first selected token to the last.</param>
/// <param name="Expression">The selected expression, or null when statements are selected.</param>
/// <param name="Statements">The selected statements, or empty when an expression is selected.</param>
public record IifeSelection(TextSpan Span, ExpressionSyntax? Expression, IReadOnlyList<StatementSyntax> Statements)
{
    public bool IsExpression => Expression is not null;

    public IEnumerable<SyntaxNode> TopLevelNodes => Expression is { } expression ? [expression] : Statements;

    /// <summary>Finds the expression or statements that <paramref name="span"/> covers exactly, ignoring whitespace and comments at its ends.</summary>
    /// <returns>Null when the span cuts through a token, or does not cover exactly one expression or whole statements of one block.</returns>
    public static IifeSelection? Find(SyntaxNode root, TextSpan span)
    {
        if (TrimToTokens(root, span) is not var (first, last))
        {
            return null;
        }

        var trimmed = TextSpan.FromBounds(first.SpanStart, last.Span.End);
        var sameSpan = first.Parent!.AncestorsAndSelf().Where(node => node.Span == trimmed).ToList();
        var statement = sameSpan.OfType<StatementSyntax>().FirstOrDefault(candidate => candidate.Parent is BlockSyntax);
        if (statement is not null)
        {
            return new IifeSelection(trimmed, null, [statement]);
        }

        var expression = sameSpan.OfType<ExpressionSyntax>().LastOrDefault();
        if (expression is not null)
        {
            return expression.Parent is ExpressionStatementSyntax { Parent: BlockSyntax } expressionStatement
                ? new IifeSelection(expressionStatement.Span, null, [expressionStatement])
                : new IifeSelection(trimmed, expression, []);
        }

        return FindStatementRun(first, last, trimmed);
    }

    static IifeSelection? FindStatementRun(SyntaxToken first, SyntaxToken last, TextSpan trimmed)
    {
        var firstStatement = first.Parent!.AncestorsAndSelf().OfType<StatementSyntax>()
            .FirstOrDefault(candidate => candidate.SpanStart == trimmed.Start && candidate.Parent is BlockSyntax);
        var lastStatement = last.Parent!.AncestorsAndSelf().OfType<StatementSyntax>()
            .FirstOrDefault(candidate => candidate.Span.End == trimmed.End && candidate.Parent is BlockSyntax);
        if (firstStatement is null || lastStatement is null || firstStatement.Parent != lastStatement.Parent)
        {
            return null;
        }

        var statements = ((BlockSyntax)firstStatement.Parent!).Statements;
        var firstIndex = statements.IndexOf(firstStatement);
        var lastIndex = statements.IndexOf(lastStatement);
        if (firstIndex > lastIndex)
        {
            return null;
        }

        return new IifeSelection(trimmed, null, [.. statements.Skip(firstIndex).Take(lastIndex - firstIndex + 1)]);
    }

    static (SyntaxToken First, SyntaxToken Last)? TrimToTokens(SyntaxNode root, TextSpan span)
    {
        if (span.IsEmpty)
        {
            return null;
        }

        var first = root.FindToken(span.Start);
        if (first.Span.End <= span.Start)
        {
            first = first.GetNextToken();
        }

        var last = root.FindToken(span.End - 1);
        if (last.SpanStart >= span.End)
        {
            last = last.GetPreviousToken();
        }

        var cutsThroughToken = first.SpanStart < span.Start || last.Span.End > span.End;
        if (first.IsKind(SyntaxKind.None) || last.IsKind(SyntaxKind.None) || cutsThroughToken || first.SpanStart > last.SpanStart)
        {
            return null;
        }

        return (first, last);
    }
}
