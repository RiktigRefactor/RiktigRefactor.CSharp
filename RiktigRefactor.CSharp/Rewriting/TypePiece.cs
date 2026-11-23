using Microsoft.CodeAnalysis;

namespace RiktigRefactor.CSharp.Rewriting;

/// <summary>A type, written so that it denotes <paramref name="Expected"/> (a symbol of the original solution).</summary>
public record TypePiece(string Text, ITypeSymbol Expected) : SourcePiece;
