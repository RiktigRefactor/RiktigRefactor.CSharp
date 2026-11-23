using Microsoft.CodeAnalysis;

namespace RiktigRefactor.CSharp.Rewriting;

/// <summary>A name an edit wrote at <paramref name="Start"/> in the new <paramref name="Document"/>.</summary>
public record WrittenName(DocumentId Document, int Start, NamePiece Piece);
