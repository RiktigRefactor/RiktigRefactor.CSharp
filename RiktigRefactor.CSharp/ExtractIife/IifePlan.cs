using Microsoft.CodeAnalysis;

namespace RiktigRefactor.CSharp.ExtractIife;

/// <summary>
/// The facts about a selection needed to write it as an immediately-invoked static lambda.
/// Computing a plan never refuses; <see cref="IifePreconditions"/> decides whether the plan is safe.
/// </summary>
/// <param name="Selection">The selected code.</param>
/// <param name="DataFlow">Roslyn's data-flow analysis of the selection.</param>
/// <param name="VariablesFromOutside">Locals and parameters declared outside the selection and read or written inside it, in order of first use.</param>
/// <param name="VariablesPassedIn">The variables from outside that are definitely assigned on entry, so they become lambda parameters.</param>
/// <param name="VariablesDeclaredInLambda">The variables from outside that are not definitely assigned on entry, so the lambda declares its own.</param>
/// <param name="VariablesReturned">Variables whose values the lambda returns: written inside and read afterwards, or declared inside and used afterwards.</param>
/// <param name="VariablesDeclaredInSelectionAndReturned">The returned variables that the selection declares, so the call site must declare them.</param>
/// <param name="InstanceType">The type of <c>this</c> when the selection uses it, or null.</param>
/// <param name="ThisRewrites">Edits that make the selection use the <c>@this</c> parameter instead of <c>this</c>.</param>
/// <param name="ExpressionType">For an expression, its type; null for statements or an expression without a type.</param>
public record IifePlan(
    IifeSelection Selection,
    DataFlowAnalysis DataFlow,
    IReadOnlyList<ISymbol> VariablesFromOutside,
    IReadOnlyList<ISymbol> VariablesPassedIn,
    IReadOnlyList<ISymbol> VariablesDeclaredInLambda,
    IReadOnlyList<ISymbol> VariablesReturned,
    IReadOnlySet<ISymbol> VariablesDeclaredInSelectionAndReturned,
    ITypeSymbol? InstanceType,
    IReadOnlyList<ThisRewrite> ThisRewrites,
    ITypeSymbol? ExpressionType)
{
    /// <summary>
    /// The types of the lambda parameters for <see cref="VariablesPassedIn"/>.
    /// A reference type is not nullable when the variable is known not to be null on entry, so a <c>var</c> local does not become a nullable parameter.
    /// </summary>
    public IReadOnlyDictionary<ISymbol, ITypeSymbol> ParameterTypes { get; init; } = new Dictionary<ISymbol, ITypeSymbol>(SymbolEqualityComparer.Default);

    /// <summary>Uses of <c>this</c> that can't be rewritten to <c>@this</c>, such as the <c>field</c> keyword.</summary>
    public IReadOnlyList<SyntaxNode> UnrewritableThisUses { get; init; } = [];

    public static ITypeSymbol TypeOf(ISymbol variable) => variable switch
    {
        ILocalSymbol local => local.Type,
        IParameterSymbol parameter => parameter.Type,
        _ => throw new ArgumentException($"`{variable}` is not a local or parameter.", nameof(variable)),
    };
}
