using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace RiktigRefactor.CSharp.Rewriting;

/// <summary>Original text, moved verbatim from where an edit deleted it. Every name in it is checked to bind as before.</summary>
public record CopyPiece(DocumentId Document, TextSpan Span) : SourcePiece;
