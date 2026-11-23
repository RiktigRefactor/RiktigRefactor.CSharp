using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RiktigRefactor.CSharp.Protocol;
using RiktigRefactor.CSharp.Verification;

namespace RiktigRefactor.CSharp.Rewriting;

/// <summary>
/// A solution rewritten by a set of <see cref="SourceEdit"/>s, with a map from every original character that survived (unchanged or moved by a <see cref="CopyPiece"/>) to where it is now.
/// An edit inside the span of a <see cref="CopyPiece"/> applies to the copy, so moved code can be changed as it moves.
/// The verifier uses the map to check that names bind as before, and the pieces to check every name an edit wrote.
/// </summary>
public class RewrittenSolution
{
    readonly Dictionary<DocumentId, List<Segment>> _segmentsByOldDocument = [];
    readonly Dictionary<DocumentId, List<Segment>> _segmentsByNewDocument = [];
    readonly List<WrittenName> _names = [];
    readonly List<WrittenType> _types = [];
    readonly Dictionary<string, (DocumentId Document, int Start)> _declarations = new(StringComparer.Ordinal);
    readonly Dictionary<DocumentId, SourceText> _texts = [];
    readonly Dictionary<CopyPiece, List<SourceEdit>> _editsInsideCopies = [];
    readonly HashSet<SourceEdit> _renderedEdits = new(ReferenceEqualityComparer.Instance);

    RewrittenSolution(Solution oldSolution)
    {
        OldSolution = oldSolution;
        NewSolution = oldSolution;
    }

    public Solution OldSolution { get; }

    public Solution NewSolution { get; private set; }

    public IReadOnlyList<FileEdit> FileEdits { get; private set; } = [];

    public IReadOnlyList<WrittenName> Names => this._names;

    public IReadOnlyList<WrittenType> Types => this._types;

    public IEnumerable<DocumentId> ChangedDocuments => this._segmentsByNewDocument.Keys;

    /// <summary>Applies <paramref name="edits"/>. Throws <see cref="InvalidOperationException"/> if they overlap, or copy text that is kept, copied twice, or not deleted.</summary>
    public static async Task<RewrittenSolution> ApplyAsync(Solution oldSolution, IEnumerable<SourceEdit> edits, CancellationToken cancellationToken)
    {
        var rewrite = new RewrittenSolution(oldSolution);
        var allEdits = edits.ToList();
        var copies = allEdits.SelectMany(edit => edit.Pieces).OfType<CopyPiece>().ToList();
        foreach (var documentId in allEdits.Select(edit => edit.Document).Concat(copies.Select(copy => copy.Document)).Distinct())
        {
            rewrite._texts[documentId] = await oldSolution.GetDocument(documentId)!.GetTextAsync(cancellationToken).ConfigureAwait(false);
        }

        CheckCopies(allEdits, copies);
        var topLevelEdits = rewrite.AssignEditsToCopies(allEdits, copies);
        var fileEdits = new List<FileEdit>();
        foreach (var group in topLevelEdits.GroupBy(edit => edit.Document))
        {
            var text = rewrite._texts[group.Key];
            var newText = rewrite.Build(group.Key, [.. group], fileEdits);
            rewrite.NewSolution = rewrite.NewSolution.WithDocumentText(group.Key, SourceText.From(newText, text.Encoding, text.ChecksumAlgorithm));
        }

        if (rewrite._renderedEdits.Count != allEdits.Count)
        {
            throw new InvalidOperationException("An edit falls inside another edit's deleted text without being inside a copy of it.");
        }

        rewrite.CheckEveryOriginalCharacterAppearsOnce();
        rewrite.FileEdits = fileEdits;
        return rewrite;
    }

    /// <summary>Where the original character at <paramref name="oldStart"/> is now, or null if it was deleted.</summary>
    public (DocumentId Document, int Start)? MapOld(DocumentId oldDocument, int oldStart)
    {
        if (!this._segmentsByOldDocument.TryGetValue(oldDocument, out var segments))
        {
            return this._texts.ContainsKey(oldDocument) ? null : (oldDocument, oldStart);
        }

        foreach (var segment in segments)
        {
            if (segment.OldStart <= oldStart && oldStart < segment.OldStart + segment.Length)
            {
                return (segment.NewDocument, segment.NewStart + oldStart - segment.OldStart);
            }
        }

        return null;
    }

    /// <summary>True if the character at <paramref name="newStart"/> is original text.</summary>
    public bool IsOriginal(DocumentId newDocument, int newStart)
        => !this._segmentsByNewDocument.TryGetValue(newDocument, out var segments)
            || segments.Any(segment => segment.NewStart <= newStart && newStart < segment.NewStart + segment.Length);

    public (DocumentId Document, int Start)? FindDeclaration(string anchor)
        => this._declarations.TryGetValue(anchor, out var found) ? found : null;

    /// <summary>An insertion at a copy's start goes into the copy; a non-empty edit must lie within it, and be smaller than it.</summary>
    static bool IsInsideCopy(CopyPiece copy, SourceEdit edit)
        => edit.Document == copy.Document
            && (edit.Span.IsEmpty
                ? copy.Span.Start <= edit.Span.Start && edit.Span.Start < copy.Span.End
                : copy.Span.Contains(edit.Span) && edit.Span != copy.Span);

    /// <summary>A copied span must have been deleted where it was, and be copied only once.</summary>
    static void CheckCopies(List<SourceEdit> edits, List<CopyPiece> copies)
    {
        foreach (var copy in copies)
        {
            if (!edits.Any(edit => edit.Document == copy.Document && edit.Span.Contains(copy.Span)))
            {
                throw new InvalidOperationException($"Copied text {copy.Span} is not deleted where it was.");
            }
        }

        for (var index = 0; index < copies.Count; index++)
        {
            for (var other = index + 1; other < copies.Count; other++)
            {
                var (first, second) = (copies[index].Span, copies[other].Span);
                var isNested = first.Contains(second) || second.Contains(first);
                if (copies[index].Document == copies[other].Document && first.OverlapsWith(second) && (!isNested || first == second))
                {
                    throw new InvalidOperationException($"Text {first} is copied twice.");
                }
            }
        }
    }

    /// <summary>A copy nested in another must be deleted inside it, or its text would appear twice.</summary>
    void CheckEveryOriginalCharacterAppearsOnce()
    {
        foreach (var (document, segments) in this._segmentsByOldDocument)
        {
            var sorted = segments.OrderBy(segment => segment.OldStart).ToList();
            for (var index = 1; index < sorted.Count; index++)
            {
                if (sorted[index].OldStart < sorted[index - 1].OldStart + sorted[index - 1].Length)
                {
                    throw new InvalidOperationException($"Original text at {sorted[index].OldStart} in {document} appears twice.");
                }
            }
        }
    }

    /// <summary>Gives each edit to the smallest copy it is inside, and returns the edits inside no copy.</summary>
    List<SourceEdit> AssignEditsToCopies(List<SourceEdit> edits, List<CopyPiece> copies)
    {
        var topLevel = new List<SourceEdit>();
        foreach (var edit in edits)
        {
            var innermost = copies.Where(copy => IsInsideCopy(copy, edit)).MinBy(copy => copy.Span.Length);
            if (innermost is null)
            {
                topLevel.Add(edit);
                continue;
            }

            if (!this._editsInsideCopies.TryGetValue(innermost, out var list))
            {
                list = [];
                this._editsInsideCopies[innermost] = list;
            }

            list.Add(edit);
        }

        return topLevel;
    }

    static List<SourceEdit> Sorted(IEnumerable<SourceEdit> edits)
    {
        var sorted = edits.Select((edit, index) => (edit, index))
            .OrderBy(pair => pair.edit.Span.Start)
            .ThenBy(pair => pair.edit.Span.Length)
            .ThenBy(pair => pair.index)
            .Select(pair => pair.edit)
            .ToList();
        for (var index = 1; index < sorted.Count; index++)
        {
            if (sorted[index].Span.Start < sorted[index - 1].Span.End)
            {
                throw new InvalidOperationException($"Edits overlap at {sorted[index].Span} and {sorted[index - 1].Span}.");
            }
        }

        return sorted;
    }

    string Build(DocumentId documentId, List<SourceEdit> edits, List<FileEdit> fileEdits)
    {
        var oldText = this._texts[documentId];
        var document = OldSolution.GetDocument(documentId)!;
        var path = document.FilePath ?? document.Name;
        var builder = new StringBuilder();
        var position = 0;
        foreach (var edit in Sorted(edits))
        {
            AppendOriginal(builder, documentId, documentId, TextSpan.FromBounds(position, edit.Span.Start));
            var editStart = builder.Length;
            AppendEdit(builder, documentId, edit);
            fileEdits.Add(new FileEdit(path, Ranges.ToPosition(oldText, edit.Span.Start), Ranges.ToPosition(oldText, edit.Span.End), builder.ToString(editStart, builder.Length - editStart)));
            position = edit.Span.End;
        }

        AppendOriginal(builder, documentId, documentId, TextSpan.FromBounds(position, oldText.Length));
        this._segmentsByNewDocument.TryAdd(documentId, []);
        return builder.ToString();
    }

    void AppendEdit(StringBuilder builder, DocumentId documentId, SourceEdit edit)
    {
        this._renderedEdits.Add(edit);
        foreach (var piece in edit.Pieces)
        {
            AppendPiece(builder, documentId, piece);
        }
    }

    void AppendPiece(StringBuilder builder, DocumentId documentId, SourcePiece piece)
    {
        switch (piece)
        {
            case LiteralPiece literal:
                builder.Append(literal.Text);
                break;
            case CopyPiece copy:
                AppendCopy(builder, documentId, copy);
                break;
            case NamePiece name:
                AppendName(builder, documentId, name);
                break;
            case TypePiece type:
                AppendType(builder, documentId, type);
                break;
            case DeclarationPiece declaration:
                AppendDeclaration(builder, documentId, declaration);
                break;
            default:
                throw new InvalidOperationException($"Unknown piece {piece}.");
        }
    }

    void AppendCopy(StringBuilder builder, DocumentId documentId, CopyPiece copy)
    {
        var position = copy.Span.Start;
        foreach (var edit in Sorted(this._editsInsideCopies.GetValueOrDefault(copy) ?? []))
        {
            AppendOriginal(builder, copy.Document, documentId, TextSpan.FromBounds(position, edit.Span.Start));
            AppendEdit(builder, documentId, edit);
            position = edit.Span.End;
        }

        AppendOriginal(builder, copy.Document, documentId, TextSpan.FromBounds(position, copy.Span.End));
    }

    void AppendName(StringBuilder builder, DocumentId documentId, NamePiece name)
    {
        this._names.Add(new WrittenName(documentId, builder.Length, name));
        builder.Append(name.Text);
    }

    void AppendType(StringBuilder builder, DocumentId documentId, TypePiece type)
    {
        this._types.Add(new WrittenType(documentId, new TextSpan(builder.Length, type.Text.Length), type.Expected));
        builder.Append(type.Text);
    }

    void AppendDeclaration(StringBuilder builder, DocumentId documentId, DeclarationPiece declaration)
    {
        if (!this._declarations.TryAdd(declaration.Anchor, (documentId, builder.Length)))
        {
            throw new InvalidOperationException($"Anchor {declaration.Anchor} is declared twice.");
        }

        builder.Append(declaration.Text);
    }

    void AppendOriginal(StringBuilder builder, DocumentId oldDocument, DocumentId newDocument, TextSpan span)
    {
        if (span.IsEmpty)
        {
            return;
        }

        var segment = new Segment(oldDocument, span.Start, span.Length, newDocument, builder.Length);
        AddSegment(this._segmentsByOldDocument, oldDocument, segment);
        AddSegment(this._segmentsByNewDocument, newDocument, segment);
        builder.Append(this._texts[oldDocument].ToString(span));
    }

    static void AddSegment(Dictionary<DocumentId, List<Segment>> segments, DocumentId documentId, Segment segment)
    {
        if (!segments.TryGetValue(documentId, out var list))
        {
            list = [];
            segments[documentId] = list;
        }

        list.Add(segment);
    }

    record Segment(DocumentId OldDocument, int OldStart, int Length, DocumentId NewDocument, int NewStart);
}
