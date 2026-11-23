namespace RiktigRefactor.CSharp.Rewriting;

/// <summary>The new declaration written by the <see cref="DeclarationPiece"/> with this anchor.</summary>
public record BindsToDeclaration(string Anchor) : NameExpectation;
