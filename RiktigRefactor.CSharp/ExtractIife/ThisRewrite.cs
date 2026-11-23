namespace RiktigRefactor.CSharp.ExtractIife;

/// <summary>Where a <c>this</c> in the selection must be rewritten to use the lambda's <c>@this</c> parameter.</summary>
/// <param name="Start">The offset in the original document.</param>
/// <param name="RemovedLength">How many characters to replace: 4 for an explicit <c>this</c>, 0 for an implicit one.</param>
/// <param name="Replacement">The text that replaces them: <c>@this</c>, or <c>@this.</c> before an implicitly qualified member name.</param>
public record ThisRewrite(int Start, int RemovedLength, string Replacement);
