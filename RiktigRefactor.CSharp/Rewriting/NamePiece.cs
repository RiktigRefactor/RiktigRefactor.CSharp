namespace RiktigRefactor.CSharp.Rewriting;

/// <summary>A simple name, which must bind as <paramref name="Expected"/> says.</summary>
public record NamePiece(string Text, NameExpectation Expected) : SourcePiece;
