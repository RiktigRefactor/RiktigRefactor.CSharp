using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RiktigRefactor.CSharp.Rewriting;

namespace RiktigRefactor.CSharp.MoveToInstanceMethod;

/// <summary>The edits that make <paramref name="Method"/> an instance method, and the original spans whose names they delete on purpose.</summary>
public record MoveToInstanceMethodPlan(IMethodSymbol Method, IReadOnlyList<SourceEdit> Edits, IReadOnlyList<(DocumentId Document, TextSpan Span)> DeletedSpans);
