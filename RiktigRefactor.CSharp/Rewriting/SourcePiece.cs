namespace RiktigRefactor.CSharp.Rewriting;

/// <summary>A part of the text a <see cref="SourceEdit"/> writes. Every name it introduces must be checked, so text with names in it is written as a <see cref="NamePiece"/> or a <see cref="TypePiece"/>.</summary>
public abstract record SourcePiece;
