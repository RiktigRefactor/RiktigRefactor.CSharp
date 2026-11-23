using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RiktigRefactor.CSharp.Protocol;
using RiktigRefactor.CSharp.Verification;

namespace RiktigRefactor.CSharp.Rewriting;

/// <summary>
/// A solution rewritten by a set of <see cref="SourceEdit"/>s, with a map from every original character that survived (unchanged or moved by a <see cref="CopyPiece"/>) to where it is now.
/// The verifier uses the map to check that names bind as before, and the pieces to check every name an edit wrote.
/// </summary>
public class RewrittenSolution
{
    readonly Dictionary<DocumentId, List<Segment>> _segmentsByOldDocument = [];
    readonly Dictionary<DocumentId, List<Segment>> _segmentsByNewDocument = [];
    readonly List<WrittenName> _names = [];
    readonly List<WrittenType> _types = [];
    readonly Dictionary<string, (DocumentId Document, int Start)> _declarations = new(StringComparer.Ordinal);

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

    /// <summary>Applies <paramref name="edits"/>. Throws <see cref="InvalidOperationException"/> if they overlap, or copy text that is kept or copied twice.</summary>
    public static async Task<RewrittenSolution> ApplyAsync(Solution oldSolution, IEnumerable<SourceEdit> edits, CancellationToken cancellationToken)
    {
        var rewrite = new RewrittenSolution(oldSolution);
        var editsByDocument = edits.GroupBy(edit => edit.Document).ToDictionary(group => group.Key, Sorted);
        var texts = new Dictionary<DocumentId, SourceText>();
        foreach (var documentId in editsByDocument.Keys.Concat(editsByDocument.Values.SelectMany(list => list).SelectMany(edit => edit.Pieces).OfType<CopyPiece>().Select(copy => copy.Document)).Distinct())
        {
            texts[documentId] = await oldSolution.GetDocument(documentId)!.GetTextAsync(cancellationToken).ConfigureAwait(false);
        }

        CheckCopies(editsByDocument);
        var fileEdits = new List<FileEdit>();
        foreach (var (documentId, documentEdits) in editsByDocument)
        {
            var newText = rewrite.Build(documentId, documentEdits, texts, fileEdits);
            rewrite.NewSolution = rewrite.NewSolution.WithDocumentText(documentId, SourceText.From(newText, texts[documentId].Encoding, texts[documentId].ChecksumAlgorithm));
        }

        rewrite.FileEdits = fileEdits;
        return rewrite;
    }

    /// <summary>Where the original character at <paramref name="oldStart"/> is now, or null if it was deleted.</summary>
    public (DocumentId Document, int Start)? MapOld(DocumentId oldDocument, int oldStart)
    {
        if (!this._segmentsByOldDocument.TryGetValue(oldDocument, out var segments))
        {
            return (oldDocument, oldStart);
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

    /// <summary>A copied span must have been deleted where it was, with no edit inside it, and be copied only once.</summary>
    static void CheckCopies(Dictionary<DocumentId, List<SourceEdit>> editsByDocument)
    {
        var copies = editsByDocument.Values.SelectMany(list => list).SelectMany(edit => edit.Pieces).OfType<CopyPiece>().ToList();
        foreach (var copy in copies)
        {
            var edits = editsByDocument.GetValueOrDefault(copy.Document) ?? [];
            var isDeleted = edits.Any(edit => edit.Span.Contains(copy.Span));
            var hasEditInside = edits.Any(edit => edit.Span != copy.Span && copy.Span.Start < edit.Span.End && edit.Span.Start < copy.Span.End && !edit.Span.Contains(copy.Span));
            if (!isDeleted || hasEditInside)
            {
                throw new InvalidOperationException($"Copied text {copy.Span} is not deleted where it was, or has an edit inside it.");
            }
        }

        for (var index = 0; index < copies.Count; index++)
        {
            for (var other = index + 1; other < copies.Count; other++)
            {
                if (copies[index].Document == copies[other].Document && copies[index].Span.OverlapsWith(copies[other].Span))
                {
                    throw new InvalidOperationException($"Text {copies[index].Span} is copied twice.");
                }
            }
        }
    }

    string Build(DocumentId documentId, List<SourceEdit> edits, Dictionary<DocumentId, SourceText> texts, List<FileEdit> fileEdits)
    {
        var oldText = texts[documentId];
        var path = OldSolution.GetDocument(documentId)!.FilePath ?? OldSolution.GetDocument(documentId)!.Name;
        var builder = new StringBuilder();
        var position = 0;
        foreach (var edit in edits)
        {
            AppendOriginal(builder, documentId, documentId, oldText, TextSpan.FromBounds(position, edit.Span.Start));
            var editStart = builder.Length;
            foreach (var piece in edit.Pieces)
            {
                AppendPiece(builder, documentId, piece, texts);
            }

            fileEdits.Add(new FileEdit(path, Ranges.ToPosition(oldText, edit.Span.Start), Ranges.ToPosition(oldText, edit.Span.End), builder.ToString(editStart, builder.Length - editStart)));
            position = edit.Span.End;
        }

        AppendOriginal(builder, documentId, documentId, oldText, TextSpan.FromBounds(position, oldText.Length));
        this._segmentsByNewDocument.TryAdd(documentId, []);
        return builder.ToString();
    }

    void AppendPiece(StringBuilder builder, DocumentId documentId, SourcePiece piece, Dictionary<DocumentId, SourceText> texts)
    {
        switch (piece)
        {
            case LiteralPiece literal:
                builder.Append(literal.Text);
                break;
            case CopyPiece copy:
                AppendOriginal(builder, copy.Document, documentId, texts[copy.Document], copy.Span);
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

    void AppendOriginal(StringBuilder builder, DocumentId oldDocument, DocumentId newDocument, SourceText oldText, TextSpan span)
    {
        if (span.IsEmpty)
        {
            return;
        }

        var segment = new Segment(oldDocument, span.Start, span.Length, newDocument, builder.Length);
        AddSegment(this._segmentsByOldDocument, oldDocument, segment);
        AddSegment(this._segmentsByNewDocument, newDocument, segment);
        builder.Append(oldText.ToString(span));
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
