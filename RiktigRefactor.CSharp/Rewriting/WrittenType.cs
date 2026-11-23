using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace RiktigRefactor.CSharp.Rewriting;

/// <summary>A type an edit wrote at <paramref name="Span"/> in the new <paramref name="Document"/>.</summary>
public record WrittenType(DocumentId Document, TextSpan Span, ITypeSymbol Expected);
