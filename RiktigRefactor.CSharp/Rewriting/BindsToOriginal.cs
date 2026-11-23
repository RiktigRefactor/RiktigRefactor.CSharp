using Microsoft.CodeAnalysis;

namespace RiktigRefactor.CSharp.Rewriting;

/// <summary>The symbol that <paramref name="Symbol"/> of the original solution became.</summary>
public record BindsToOriginal(ISymbol Symbol) : NameExpectation;
