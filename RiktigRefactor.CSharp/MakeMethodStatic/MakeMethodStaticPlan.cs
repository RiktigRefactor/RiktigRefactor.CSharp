using Microsoft.CodeAnalysis;
using RiktigRefactor.CSharp.Rewriting;

namespace RiktigRefactor.CSharp.MakeMethodStatic;

/// <summary>The edits that make <paramref name="Method"/> static: its declaration, its body, and every call.</summary>
public record MakeMethodStaticPlan(IMethodSymbol Method, IReadOnlyList<SourceEdit> Edits);
