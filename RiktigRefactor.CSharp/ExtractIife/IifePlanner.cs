using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace RiktigRefactor.CSharp.ExtractIife;

public static class IifePlanner
{
    /// <returns>Null when Roslyn can't analyze the data flow of the selection.</returns>
    public static IifePlan? Plan(SemanticModel model, IifeSelection selection, CancellationToken cancellationToken)
    {
        var dataFlow = selection.Expression is { } expression
            ? model.AnalyzeDataFlow(expression)
            : model.AnalyzeDataFlow(selection.Statements[0], selection.Statements[^1]);
        if (dataFlow is null || !dataFlow.Succeeded)
        {
            return null;
        }

        var firstUse = FirstUses(model, selection, cancellationToken);
        int OrderOfUse(ISymbol variable) => firstUse.TryGetValue(variable, out var name) ? name.SpanStart : int.MaxValue;

        var declaredInside = dataFlow.VariablesDeclared.ToHashSet(SymbolEqualityComparer.Default);
        var fromOutside = dataFlow.ReadInside.Concat(dataFlow.WrittenInside)
            .Distinct(SymbolEqualityComparer.Default)
            .Where(variable => variable is not null && !declaredInside.Contains(variable) && variable is not IParameterSymbol { IsThis: true })
            .Select(variable => variable!)
            .OrderBy(OrderOfUse)
            .ToList();
        var definitelyAssigned = dataFlow.DefinitelyAssignedOnEntry.ToHashSet(SymbolEqualityComparer.Default);
        var passedIn = fromOutside.Where(definitelyAssigned.Contains).ToList();
        var declaredInLambda = fromOutside.Where(variable => !definitelyAssigned.Contains(variable)).ToList();

        var usedAfter = dataFlow.ReadOutside.Concat(dataFlow.WrittenOutside).ToHashSet(SymbolEqualityComparer.Default);
        var flowsOut = dataFlow.DataFlowsOut.ToHashSet(SymbolEqualityComparer.Default);
        var declaredAndReturned = declaredInside.Where(usedAfter.Contains).ToHashSet(SymbolEqualityComparer.Default);
        var returned = fromOutside.Where(flowsOut.Contains)
            .Concat(declaredAndReturned)
            .OrderBy(OrderOfUse)
            .ToList();

        var (instanceType, rewrites, unrewritable) = FindThisUses(model, selection, cancellationToken);
        var expressionType = selection.Expression is { } selected ? model.GetTypeInfo(selected, cancellationToken).Type : null;

        var parameterTypes = passedIn.ToDictionary(
            variable => variable,
            variable => ParameterType(model, variable, firstUse.GetValueOrDefault(variable), cancellationToken),
            SymbolEqualityComparer.Default);

        return new IifePlan(selection, dataFlow, fromOutside, passedIn, declaredInLambda, returned, declaredAndReturned, instanceType, rewrites, expressionType)
        {
            UnrewritableThisUses = unrewritable,
            ParameterTypes = parameterTypes,
        };
    }

    /// <summary>All operations in the selection, including those in nested lambdas and local functions.</summary>
    public static IEnumerable<IOperation> Operations(SemanticModel model, IifeSelection selection, CancellationToken cancellationToken)
        => selection.TopLevelNodes
            .Select(node => model.GetOperation(node, cancellationToken))
            .Where(operation => operation is not null)
            .SelectMany(operation => operation!.DescendantsAndSelf());

    static ITypeSymbol ParameterType(SemanticModel model, ISymbol variable, IdentifierNameSyntax? firstUse, CancellationToken cancellationToken)
    {
        var declaredType = IifePlan.TypeOf(variable);
        var isKnownNotNull = firstUse is not null && model.GetTypeInfo(firstUse, cancellationToken).Nullability.FlowState == NullableFlowState.NotNull;
        return declaredType.IsReferenceType && isKnownNotNull ? declaredType.WithNullableAnnotation(NullableAnnotation.NotAnnotated) : declaredType;
    }

    static Dictionary<ISymbol, IdentifierNameSyntax> FirstUses(SemanticModel model, IifeSelection selection, CancellationToken cancellationToken)
    {
        var offsets = new Dictionary<ISymbol, IdentifierNameSyntax>(SymbolEqualityComparer.Default);
        var names = selection.TopLevelNodes.SelectMany(node => node.DescendantNodesAndSelf()).OfType<IdentifierNameSyntax>();
        foreach (var name in names)
        {
            if (model.GetSymbolInfo(name, cancellationToken).Symbol is { } symbol && !offsets.ContainsKey(symbol))
            {
                offsets[symbol] = name;
            }
        }

        return offsets;
    }

    static (ITypeSymbol? InstanceType, List<ThisRewrite> Rewrites, List<SyntaxNode> Unrewritable) FindThisUses(
        SemanticModel model,
        IifeSelection selection,
        CancellationToken cancellationToken)
    {
        ITypeSymbol? instanceType = null;
        var rewrites = new Dictionary<int, ThisRewrite>();
        var unrewritable = new List<SyntaxNode>();
        var thisReferences = Operations(model, selection, cancellationToken)
            .OfType<IInstanceReferenceOperation>()
            .Where(reference => reference.ReferenceKind == InstanceReferenceKind.ContainingTypeInstance);
        foreach (var reference in thisReferences)
        {
            instanceType ??= reference.Type;
            var rewrite = RewriteFor(reference);
            if (rewrite is null)
            {
                unrewritable.Add(reference.Syntax);
            }
            else
            {
                rewrites[rewrite.Start] = rewrite;
            }
        }

        return (instanceType, [.. rewrites.Values.OrderBy(rewrite => rewrite.Start)], unrewritable);
    }

    static ThisRewrite? RewriteFor(IInstanceReferenceOperation reference)
    {
        if (!reference.IsImplicit)
        {
            return reference.Syntax is ThisExpressionSyntax explicitThis
                ? new ThisRewrite(explicitThis.SpanStart, explicitThis.Span.Length, "@this")
                : null;
        }

        var name = reference.Syntax switch
        {
            SimpleNameSyntax simpleName => simpleName,
            InvocationExpressionSyntax { Expression: SimpleNameSyntax invokedName } => invokedName,
            _ => null,
        };
        if (name is null || (name.Parent is MemberAccessExpressionSyntax access && access.Name == name))
        {
            return null;
        }

        return new ThisRewrite(name.SpanStart, 0, "@this.");
    }
}
