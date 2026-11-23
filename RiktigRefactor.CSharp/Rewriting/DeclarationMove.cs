using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace RiktigRefactor.CSharp.Rewriting;

/// <summary>
/// Writes the edits that move a member declaration to the end of a type declaration: delete it where it is, and copy it before the type's closing brace, re-indented.
/// Other edits inside the declaration go with the copy.
/// </summary>
public static class DeclarationMove
{
    public static async Task<List<SourceEdit>> PlanAsync(Solution solution, MemberDeclarationSyntax declaration, TypeDeclarationSyntax targetDeclaration, CancellationToken cancellationToken)
    {
        var sourceDocument = solution.GetDocumentId(declaration.SyntaxTree)!;
        var targetDocument = solution.GetDocumentId(targetDeclaration.SyntaxTree)!;
        var sourceText = await solution.GetDocument(sourceDocument)!.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var targetText = await solution.GetDocument(targetDocument)!.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var newline = targetText.ToString().Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";

        var fullSpan = declaration.FullSpan;
        var firstLineStart = FirstNonBlankLineStart(sourceText, fullSpan);
        var sourceIndent = Indentation(sourceText, firstLineStart);
        var targetIndent = TargetMemberIndentation(targetText, targetDeclaration);
        var edits = ReindentLines(sourceDocument, declaration, sourceText, TextSpan.FromBounds(firstLineStart, fullSpan.End), sourceIndent, targetIndent, cancellationToken);

        var moved = new List<SourcePiece>();
        var closeBrace = targetDeclaration.CloseBraceToken;
        var closeBraceLine = targetText.Lines.GetLineFromPosition(closeBrace.SpanStart);
        var braceStartsItsLine = string.IsNullOrWhiteSpace(targetText.ToString(TextSpan.FromBounds(closeBraceLine.Start, closeBrace.SpanStart)));
        if (!braceStartsItsLine || targetDeclaration.Members.Count > 0)
        {
            moved.Add(new LiteralPiece(newline));
        }

        moved.Add(new CopyPiece(sourceDocument, TextSpan.FromBounds(firstLineStart, fullSpan.End)));
        if (!sourceText.ToString(fullSpan).EndsWith('\n'))
        {
            moved.Add(new LiteralPiece(newline));
        }

        if (!braceStartsItsLine)
        {
            moved.Add(new LiteralPiece(Indentation(targetText, targetText.Lines.GetLineFromPosition(targetDeclaration.SpanStart).Start)));
        }

        edits.Add(SourceEdit.Delete(sourceDocument, fullSpan));
        var whitespaceBeforeBrace = TextSpan.FromBounds(closeBrace.GetPreviousToken().Span.End, closeBrace.SpanStart);
        var insertedSpan = braceStartsItsLine ? new TextSpan(closeBraceLine.Start, 0) : whitespaceBeforeBrace;
        edits.Add(SourceEdit.Replace(targetDocument, insertedSpan, [.. moved]));
        return edits;
    }

    static int FirstNonBlankLineStart(SourceText text, TextSpan span)
    {
        var line = text.Lines.GetLineFromPosition(span.Start);
        var start = Math.Max(line.Start, span.Start);
        while (line.LineNumber + 1 < text.Lines.Count && line.End < span.End && string.IsNullOrWhiteSpace(text.ToString(TextSpan.FromBounds(start, line.End))))
        {
            line = text.Lines[line.LineNumber + 1];
            start = line.Start;
        }

        return start;
    }

    static string Indentation(SourceText text, int lineStart)
    {
        var end = lineStart;
        while (end < text.Length && text[end] is ' ' or '\t')
        {
            end++;
        }

        return text.ToString(TextSpan.FromBounds(lineStart, end));
    }

    static string TargetMemberIndentation(SourceText targetText, TypeDeclarationSyntax targetDeclaration)
    {
        if (targetDeclaration.Members.Count > 0)
        {
            var first = targetDeclaration.Members[0];
            return Indentation(targetText, targetText.Lines.GetLineFromPosition(first.SpanStart).Start);
        }

        return Indentation(targetText, targetText.Lines.GetLineFromPosition(targetDeclaration.SpanStart).Start) + "    ";
    }

    /// <summary>Changes each line's indentation, except lines that continue a multi-line string, whose whitespace is part of the string.</summary>
    static List<SourceEdit> ReindentLines(DocumentId documentId, SyntaxNode declaration, SourceText text, TextSpan span, string sourceIndent, string targetIndent, CancellationToken cancellationToken)
    {
        var edits = new List<SourceEdit>();
        if (sourceIndent == targetIndent)
        {
            return edits;
        }

        var root = declaration.SyntaxTree.GetRoot(cancellationToken);
        foreach (var line in text.Lines.Where(line => span.Contains(line.Start) && line.Start < line.End))
        {
            var token = root.FindToken(line.Start, findInsideTrivia: true);
            var isInsideToken = token.Span.Start < line.Start && line.Start < token.Span.End;
            var isInsideInterpolatedString = token.Parent?.AncestorsAndSelf().Any(node => node is InterpolatedStringExpressionSyntax interpolated && interpolated.SpanStart < line.Start) == true;
            if (isInsideToken || isInsideInterpolatedString || !text.ToString(line.Span).StartsWith(sourceIndent, StringComparison.Ordinal))
            {
                continue;
            }

            edits.Add(SourceEdit.Replace(documentId, new TextSpan(line.Start, sourceIndent.Length), new LiteralPiece(targetIndent)));
        }

        return edits;
    }
}
