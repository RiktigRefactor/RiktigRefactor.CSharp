namespace RiktigRefactor.CSharp.Rewriting;

/// <summary>Text with no names in it, such as punctuation, keywords and whitespace.</summary>
public record LiteralPiece(string Text) : SourcePiece;
