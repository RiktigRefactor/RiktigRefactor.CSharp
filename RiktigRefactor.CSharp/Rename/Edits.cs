using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RiktigRefactor.CSharp.Protocol;
using RiktigRefactor.CSharp.Verification;

namespace RiktigRefactor.CSharp.Rename;

public static class Edits
{
    /// <summary>
    /// Computes the edits that turn <paramref name="oldSolution"/> into <paramref name="newSolution"/>.
    /// Roslyn's text differ may split a renamed identifier (<c>Double</c> to <c>Twice</c> becomes <c>Doubl</c> to <c>Twic</c>),
    /// so each edit is widened to cover whole tokens.
    /// </summary>
    public static async Task<IReadOnlyList<FileEdit>> ComputeAsync(Solution oldSolution, Solution newSolution, CancellationToken cancellationToken)
    {
        var edits = new List<FileEdit>();
        foreach (var projectChanges in newSolution.GetChanges(oldSolution).GetProjectChanges())
        {
            foreach (var documentId in projectChanges.GetChangedDocuments(onlyGetDocumentsWithTextChanges: true))
            {
                var oldDocument = oldSolution.GetDocument(documentId)!;
                var newDocument = newSolution.GetDocument(documentId)!;
                var oldText = await oldDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var oldRoot = await oldDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var changes = await newDocument.GetTextChangesAsync(oldDocument, cancellationToken).ConfigureAwait(false);
                var path = oldDocument.FilePath ?? oldDocument.Name;
                foreach (var (span, newText) in WidenToTokens(oldText, oldRoot!, changes))
                {
                    edits.Add(new FileEdit(path, Ranges.ToPosition(oldText, span.Start), Ranges.ToPosition(oldText, span.End), newText));
                }
            }
        }

        return edits;
    }

    static List<(TextSpan Span, string NewText)> WidenToTokens(SourceText oldText, SyntaxNode oldRoot, IEnumerable<TextChange> changes)
    {
        var groups = new List<(TextSpan Span, List<TextChange> Changes)>();
        foreach (var change in changes.OrderBy(change => change.Span.Start))
        {
            var widened = Widen(oldRoot, change.Span);
            if (groups.Count > 0 && widened.Start < groups[^1].Span.End)
            {
                var last = groups[^1];
                last.Changes.Add(change);
                groups[^1] = (TextSpan.FromBounds(last.Span.Start, Math.Max(last.Span.End, widened.End)), last.Changes);
            }
            else
            {
                groups.Add((widened, [change]));
            }
        }

        return [.. groups.Select(group => (group.Span, Apply(oldText, group.Span, group.Changes)))];
    }

    static TextSpan Widen(SyntaxNode root, TextSpan span)
    {
        var start = span.Start;
        var end = span.End;
        var first = root.FindToken(start);
        if (first.Span.Contains(start) || first.SpanStart == start)
        {
            start = Math.Min(start, first.SpanStart);
            end = span.IsEmpty ? Math.Max(end, first.Span.End) : end;
        }

        if (!span.IsEmpty)
        {
            var last = root.FindToken(span.End - 1);
            if (last.Span.Contains(span.End - 1))
            {
                end = Math.Max(end, last.Span.End);
            }
        }

        return TextSpan.FromBounds(start, end);
    }

    static string Apply(SourceText oldText, TextSpan widened, List<TextChange> changes)
    {
        var text = oldText.ToString(widened);
        foreach (var change in changes.OrderByDescending(change => change.Span.Start))
        {
            var relativeStart = change.Span.Start - widened.Start;
            text = text.Remove(relativeStart, change.Span.Length).Insert(relativeStart, change.NewText ?? string.Empty);
        }

        return text;
    }
}
