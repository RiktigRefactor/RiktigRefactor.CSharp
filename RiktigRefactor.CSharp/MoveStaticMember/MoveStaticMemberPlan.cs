using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RiktigRefactor.CSharp.Rewriting;

namespace RiktigRefactor.CSharp.MoveStaticMember;

/// <summary>The edits that move <paramref name="Member"/>, and the original spans of the qualifiers they replace.</summary>
public record MoveStaticMemberPlan(ISymbol Member, IReadOnlyList<SourceEdit> Edits, IReadOnlyList<(DocumentId Document, TextSpan Span)> ReplacedQualifiers);
