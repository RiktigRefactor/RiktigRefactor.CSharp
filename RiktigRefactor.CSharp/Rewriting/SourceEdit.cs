using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace RiktigRefactor.CSharp.Rewriting;

/// <summary>Replaces <paramref name="Span"/> of a document of the original solution with <paramref name="Pieces"/>.</summary>
public record SourceEdit(DocumentId Document, TextSpan Span, IReadOnlyList<SourcePiece> Pieces)
{
    public static SourceEdit Insert(DocumentId document, int position, params SourcePiece[] pieces)
        => new(document, new TextSpan(position, 0), pieces);

    public static SourceEdit Replace(DocumentId document, TextSpan span, params SourcePiece[] pieces)
        => new(document, span, pieces);

    public static SourceEdit Delete(DocumentId document, TextSpan span)
        => new(document, span, []);
}
