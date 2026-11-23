namespace RiktigRefactor.CSharp.Rewriting;

/// <summary>An identifier that declares something new; names can expect to bind to it through <paramref name="Anchor"/>.</summary>
public record DeclarationPiece(string Text, string Anchor) : SourcePiece;
