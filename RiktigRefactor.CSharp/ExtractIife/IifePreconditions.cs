using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using RiktigRefactor.CSharp.Protocol;
using RiktigRefactor.CSharp.Verification;

namespace RiktigRefactor.CSharp.ExtractIife;

/// <summary>The conditions under which wrapping the selection in a static lambda is known to keep behavior.</summary>
public static class IifePreconditions
{
    const int MaxDelegateParameters = 16;

    public static RefusalReason? FindProblem(SemanticModel model, IifePlan plan, CancellationToken cancellationToken)
        => FindSyntaxProblem(plan.Selection)
            ?? FindContextProblem(model, plan.Selection, cancellationToken)
            ?? FindControlFlowProblem(model, plan.Selection)
            ?? FindThisProblem(plan)
            ?? FindExpressionProblem(model, plan, cancellationToken)
            ?? FindStatementProblem(plan)
            ?? FindVariableProblem(model, plan, cancellationToken);

    static RefusalReason? FindSyntaxProblem(IifeSelection selection)
    {
        var nodes = selection.TopLevelNodes.SelectMany(node => node.DescendantNodesAndSelf()).ToList();
        var directive = selection.TopLevelNodes
            .SelectMany(node => node.DescendantTrivia())
            .FirstOrDefault(trivia => (trivia.IsDirective || trivia.IsKind(SyntaxKind.DisabledTextTrivia)) && selection.Span.Contains(trivia.Span));
        if (directive != default)
        {
            return Refuse(RefusalCodes.PreprocessorRegion, "The selection contains a preprocessor directive.", directive.SyntaxTree!, directive.Span);
        }

        if (nodes.OfType<BaseExpressionSyntax>().FirstOrDefault() is { } baseAccess)
        {
            return Refuse(RefusalCodes.BaseAccess, "The selection uses `base`, which a static lambda can't.", baseAccess, "Shrink the selection to exclude the `base` access.");
        }

        if (nodes.FirstOrDefault(IsUnsafeOrRefOnly) is { } unsafeNode)
        {
            return Refuse(RefusalCodes.RefOrPointerAcrossBoundary, $"The selection uses `{Truncate(unsafeNode)}`, which can't move into a lambda safely.", unsafeNode, null);
        }

        var outsideNestedFunctions = selection.TopLevelNodes.SelectMany(node => node.DescendantNodesAndSelf(descendIntoChildren: child => !IsFunction(child) || child == node)).ToList();
        if (outsideNestedFunctions.FirstOrDefault(IsAwait) is { } awaitNode)
        {
            return Refuse(RefusalCodes.ContainsAwait, "The selection contains `await`, which a non-async lambda can't.", awaitNode, null);
        }

        var jump = outsideNestedFunctions.FirstOrDefault(node => node is YieldStatementSyntax or LabeledStatementSyntax || node.IsKind(SyntaxKind.GotoStatement));
        if (jump is not null)
        {
            return Refuse(RefusalCodes.NonLocalControlFlow, $"The selection contains `{Truncate(jump)}`, which can't move into a lambda.", jump, null);
        }

        return null;
    }

    static RefusalReason? FindContextProblem(SemanticModel model, IifeSelection selection, CancellationToken cancellationToken)
    {
        var first = selection.TopLevelNodes.First();
        foreach (var ancestor in first.Ancestors())
        {
            var isQueryClause = ancestor is QueryExpressionSyntax;
            var isExpressionTreeLambda = ancestor is AnonymousFunctionExpressionSyntax lambda && IsExpressionTree(model.GetTypeInfo(lambda, cancellationToken).ConvertedType);
            if (isQueryClause || isExpressionTreeLambda)
            {
                return Refuse(
                    RefusalCodes.ExpressionTreeOrQuery,
                    "The selection is inside a query expression or an expression tree, where a lambda call would change what the query or tree contains.",
                    ancestor,
                    "Select the whole query or expression tree lambda instead.");
            }
        }

        return null;
    }

    static RefusalReason? FindControlFlowProblem(SemanticModel model, IifeSelection selection)
    {
        if (selection.IsExpression)
        {
            return null;
        }

        var controlFlow = model.AnalyzeControlFlow(selection.Statements[0], selection.Statements[^1]);
        if (controlFlow is null || !controlFlow.Succeeded)
        {
            return Refuse(RefusalCodes.InvalidSelection, "Roslyn could not analyze the control flow of the selection.", selection.Statements[0], null);
        }

        if (controlFlow.ExitPoints.FirstOrDefault() is { } exit)
        {
            return Refuse(
                RefusalCodes.NonLocalControlFlow,
                $"`{Truncate(exit)}` leaves the selection, which it could not do from inside a lambda.",
                exit,
                "Shrink the selection to exclude it, or restructure the code so control leaves only at the end.");
        }

        if (controlFlow.EntryPoints.FirstOrDefault() is { } entry)
        {
            return Refuse(RefusalCodes.NonLocalControlFlow, "A jump from outside enters the selection.", entry, null);
        }

        return null;
    }

    static RefusalReason? FindThisProblem(IifePlan plan)
    {
        if (plan.UnrewritableThisUses is [var unrewritable, ..])
        {
            return Refuse(RefusalCodes.ThisNotRewritable, $"`{Truncate(unrewritable)}` uses `this` in a way that can't be passed to a static lambda.", unrewritable, null);
        }

        if (plan.InstanceType is { IsReferenceType: false })
        {
            return Refuse(
                RefusalCodes.StructThis,
                "The selection uses `this` in a struct; passing it to a lambda would copy it.",
                plan.Selection.TopLevelNodes.First(),
                null);
        }

        return null;
    }

    static RefusalReason? FindExpressionProblem(SemanticModel model, IifePlan plan, CancellationToken cancellationToken)
    {
        if (plan.Selection.Expression is not { } expression)
        {
            return null;
        }

        if (IsNotAValue(model, expression, cancellationToken))
        {
            return Refuse(RefusalCodes.InvalidSelection, $"`{Truncate(expression)}` is not a value that a lambda could return.", expression, null);
        }

        var operation = model.GetOperation(expression, cancellationToken);
        if (IsVariable(operation) || IsWrittenTo(expression))
        {
            return Refuse(
                RefusalCodes.SelectionIsVariable,
                $"`{Truncate(expression)}` denotes a variable; a lambda would return a copy of its value instead.",
                expression,
                "Select a larger expression.");
        }

        if (plan.VariablesFromOutside.FirstOrDefault(plan.DataFlow.WrittenInside.Contains) is { } written)
        {
            return Refuse(
                RefusalCodes.ExpressionWritesVariable,
                $"The expression assigns `{written.Name}`, which the lambda would only change in its own copy.",
                expression,
                "Select whole statements instead, so the new value can be returned.");
        }

        if (plan.VariablesDeclaredInSelectionAndReturned.FirstOrDefault() is { } escaping)
        {
            return Refuse(RefusalCodes.DeclarationEscapes, $"The expression declares `{escaping.Name}`, which is used after it.", expression, null);
        }

        var typeInfo = model.GetTypeInfo(expression, cancellationToken);
        if (typeInfo.Type is null || typeInfo.Type.SpecialType == SpecialType.System_Void || !KeepsConversion(model, expression, typeInfo, cancellationToken))
        {
            return Refuse(
                RefusalCodes.TargetTypedExpression,
                $"The meaning of `{Truncate(expression)}` depends on where it is used, so returning it from a lambda could change it.",
                expression,
                "Select a larger expression.");
        }

        if (UnnameableTypePart(typeInfo.Type) is { } unnameable)
        {
            return Refuse(RefusalCodes.TypeNotNameable, $"The expression's type `{unnameable.ToDisplayString()}` can't be written as a delegate type argument.", expression, null);
        }

        return null;
    }

    static RefusalReason? FindStatementProblem(IifePlan plan)
    {
        var selection = plan.Selection;
        if (selection.IsExpression)
        {
            return null;
        }

        var usingDeclaration = selection.Statements.OfType<LocalDeclarationStatementSyntax>().FirstOrDefault(declaration => declaration.UsingKeyword != default);
        if (usingDeclaration is not null)
        {
            return Refuse(
                RefusalCodes.DisposalWouldMove,
                "A `using` declaration in the selection would be disposed at the end of the lambda instead of the end of its block.",
                usingDeclaration,
                "Extend the selection to the end of the block, or use a `using` statement.");
        }

        var assignedOnExit = plan.DataFlow.DefinitelyAssignedOnExit.ToHashSet(SymbolEqualityComparer.Default);
        if (plan.VariablesDeclaredInSelectionAndReturned.FirstOrDefault(variable => !assignedOnExit.Contains(variable)) is { } unassigned)
        {
            return Refuse(
                RefusalCodes.DeclarationEscapes,
                $"`{unassigned.Name}` is declared in the selection and used after it, but is not definitely assigned at its end, so the lambda can't return it.",
                selection.Statements[0],
                null);
        }

        var writesOutside = plan.VariablesReturned.Where(variable => !plan.VariablesDeclaredInSelectionAndReturned.Contains(variable)).ToList();
        var enclosingTry = selection.Statements[0].Ancestors().TakeWhile(node => !IsFunction(node)).OfType<TryStatementSyntax>().FirstOrDefault();
        if (writesOutside.Count > 0 && enclosingTry is not null)
        {
            return Refuse(
                RefusalCodes.WritesLostOnException,
                $"The selection assigns `{writesOutside[0].Name}`, which is read afterwards. Inside a `try`, an exception thrown after that assignment would lose it, because the lambda returns new values only when it completes.",
                selection.Statements[0],
                null);
        }

        return null;
    }

    static RefusalReason? FindVariableProblem(SemanticModel model, IifePlan plan, CancellationToken cancellationToken)
    {
        var at = plan.Selection.TopLevelNodes.First();
        var captured = plan.DataFlow.CapturedInside.Concat(plan.DataFlow.CapturedOutside).ToHashSet(SymbolEqualityComparer.Default);
        foreach (var variable in plan.VariablesFromOutside.Concat(plan.VariablesReturned))
        {
            if (variable is not (ILocalSymbol or IParameterSymbol))
            {
                return Refuse(RefusalCodes.ExpressionTreeOrQuery, $"`{variable.Name}` is not a local or parameter, so it can't be passed to a lambda.", at, null);
            }

            if (variable is ILocalSymbol { RefKind: not RefKind.None } or IParameterSymbol { RefKind: not RefKind.None } or ILocalSymbol { IsFixed: true })
            {
                return Refuse(RefusalCodes.RefOrPointerAcrossBoundary, $"`{variable.Name}` is a reference to another variable; a lambda would copy its value instead.", at, null);
            }

            if (captured.Contains(variable) || IsPrimaryConstructorParameter(variable))
            {
                return Refuse(
                    RefusalCodes.CapturedVariable,
                    $"`{variable.Name}` is captured by a lambda or local function, which would not see the changes the extracted code makes to its own copy.",
                    at,
                    null);
            }

            if (UnnameableTypePart(IifePlan.TypeOf(variable)) is { } unnameable)
            {
                var code = IsRefOrPointer(unnameable) ? RefusalCodes.RefOrPointerAcrossBoundary : RefusalCodes.TypeNotNameable;
                return Refuse(code, $"`{variable.Name}` has type `{unnameable.ToDisplayString()}`, which can't be passed to a lambda.", at, null);
            }
        }

        if (FindPossibleStructMutation(model, plan, cancellationToken) is { } mutation)
        {
            return Refuse(
                RefusalCodes.StructMutationPossible,
                $"`{Truncate(mutation)}` may change a struct that the lambda would only have a copy of.",
                mutation,
                null);
        }

        var parameterCount = plan.VariablesPassedIn.Count + (plan.InstanceType is null ? 0 : 1);
        var needsReturnSlot = plan.Selection.IsExpression || plan.VariablesReturned.Count > 0;
        if (parameterCount > MaxDelegateParameters || (parameterCount == MaxDelegateParameters + 1 && needsReturnSlot))
        {
            return Refuse(RefusalCodes.TooManyVariables, $"The lambda would need {parameterCount} parameters; `Func` and `Action` allow at most {MaxDelegateParameters}.", at, "Select less code.");
        }

        return null;
    }

    /// <summary>
    /// Finds a use of a variable from outside, of a mutable struct type, that could change it although Roslyn's data-flow analysis did not count a write to it:
    /// calling a non-readonly member on it (or on a field of it), or assigning a field of it.
    /// The lambda would change only its copy, and not return it.
    /// </summary>
    static SyntaxNode? FindPossibleStructMutation(SemanticModel model, IifePlan plan, CancellationToken cancellationToken)
    {
        var written = plan.DataFlow.WrittenInside.ToHashSet(SymbolEqualityComparer.Default);
        var fromOutsideAndNotWritten = plan.VariablesFromOutside.Where(variable => !written.Contains(variable)).ToHashSet(SymbolEqualityComparer.Default);
        foreach (var operation in IifePlanner.Operations(model, plan.Selection, cancellationToken))
        {
            var variable = operation switch
            {
                ILocalReferenceOperation local => (ISymbol)local.Local,
                IParameterReferenceOperation parameter => parameter.Parameter,
                _ => null,
            };
            if (variable is null || !fromOutsideAndNotWritten.Contains(variable) || !IsMutableValueType(operation.Type))
            {
                continue;
            }

            var outermost = operation;
            while (outermost.Parent is IFieldReferenceOperation field && field.Instance == outermost && IsMutableValueType(field.Type))
            {
                outermost = field;
            }

            if (MayMutateThroughReceiver(outermost) || (outermost != operation && IsAssignedTo(outermost)))
            {
                return operation.Syntax.Parent ?? operation.Syntax;
            }
        }

        return null;
    }

    static bool MayMutateThroughReceiver(IOperation receiver) => receiver.Parent switch
    {
        IInvocationOperation invocation when invocation.Instance == receiver => !invocation.TargetMethod.IsReadOnly,
        IPropertyReferenceOperation property when property.Instance == receiver => !IsReadOnlyProperty(property.Property),
        IEventReferenceOperation eventReference when eventReference.Instance == receiver => true,
        IFieldReferenceOperation field when field.Instance == receiver => IsAssignedTo(field),
        _ => false,
    };

    /// <summary>Members of the type can read and write a primary constructor parameter, like a field, so a copy could go stale.</summary>
    static bool IsPrimaryConstructorParameter(ISymbol variable)
        => variable is IParameterSymbol { ContainingSymbol: IMethodSymbol { MethodKind: MethodKind.Constructor } constructor }
            && constructor.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax() is TypeDeclarationSyntax);

    static bool IsReadOnlyProperty(IPropertySymbol property)
        => (property.GetMethod?.IsReadOnly ?? true) && (property.SetMethod?.IsReadOnly ?? true);

    static bool IsAssignedTo(IOperation operation) => operation.Parent switch
    {
        IAssignmentOperation assignment => assignment.Target == operation,
        IIncrementOrDecrementOperation => true,
        IArgumentOperation argument => argument.Parameter?.RefKind is RefKind.Ref or RefKind.Out,
        _ => false,
    };

    static bool IsMutableValueType(ITypeSymbol? type) => type switch
    {
        null => false,
        ITypeParameterSymbol typeParameter => !typeParameter.IsReferenceType,
        _ => type.IsValueType && !type.IsReadOnly && type.TypeKind != TypeKind.Enum,
    };

    static bool KeepsConversion(SemanticModel model, ExpressionSyntax expression, TypeInfo typeInfo, CancellationToken cancellationToken)
    {
        var conversion = model.GetConversion(expression, cancellationToken);
        if (conversion.IsIdentity)
        {
            return true;
        }

        var isConstant = model.GetConstantValue(expression, cancellationToken).HasValue;
        var isPlainImplicit = conversion.IsImplicit && !conversion.IsUserDefined
            && (conversion.IsNumeric || conversion.IsReference || conversion.IsBoxing || conversion.IsNullable);
        return typeInfo.ConvertedType is not null && !isConstant && isPlainImplicit;
    }

    static bool IsNotAValue(SemanticModel model, ExpressionSyntax expression, CancellationToken cancellationToken)
    {
        var isNamePart = expression.Parent switch
        {
            MemberAccessExpressionSyntax access => access.Name == expression,
            QualifiedNameSyntax or AliasQualifiedNameSyntax or MemberBindingExpressionSyntax or NameColonSyntax or NameEqualsSyntax => true,
            _ => false,
        };
        var symbol = model.GetSymbolInfo(expression, cancellationToken).Symbol;
        return isNamePart || symbol is ITypeSymbol or INamespaceSymbol || expression is DeclarationExpressionSyntax or RefExpressionSyntax;
    }

    static bool IsVariable(IOperation? operation) => operation switch
    {
        ILocalReferenceOperation or IParameterReferenceOperation or IFieldReferenceOperation or IArrayElementReferenceOperation => true,
        IInstanceReferenceOperation or IEventReferenceOperation or IInlineArrayAccessOperation or IImplicitIndexerReferenceOperation => true,
        IPropertyReferenceOperation property => property.Property.ReturnsByRef || property.Property.ReturnsByRefReadonly,
        IInvocationOperation invocation => invocation.TargetMethod.ReturnsByRef || invocation.TargetMethod.ReturnsByRefReadonly,
        IConditionalOperation conditional => conditional.IsRef,
        IDeclarationExpressionOperation or IDiscardOperation => true,
        _ => false,
    };

    static bool IsWrittenTo(ExpressionSyntax expression) => expression.Parent switch
    {
        AssignmentExpressionSyntax assignment => assignment.Left == expression,
        PrefixUnaryExpressionSyntax prefix => prefix.IsKind(SyntaxKind.PreIncrementExpression) || prefix.IsKind(SyntaxKind.PreDecrementExpression) || prefix.IsKind(SyntaxKind.AddressOfExpression),
        PostfixUnaryExpressionSyntax => true,
        ArgumentSyntax argument => argument.RefKindKeyword != default,
        _ => false,
    };

    /// <summary>The part of <paramref name="type"/> that can't be a type argument of <c>Func</c> or <c>Action</c>, or null if all of it can.</summary>
    static ITypeSymbol? UnnameableTypePart(ITypeSymbol type)
    {
        if (type.IsAnonymousType || IsRefOrPointer(type) || type.TypeKind == TypeKind.Error || type.SpecialType == SpecialType.System_Void)
        {
            return type;
        }

        if (type is ITypeParameterSymbol { AllowsRefLikeType: true })
        {
            return type;
        }

        var parts = type switch
        {
            INamedTypeSymbol named => named.TypeArguments,
            IArrayTypeSymbol array => [array.ElementType],
            _ => [],
        };
        return parts.Select(UnnameableTypePart).FirstOrDefault(part => part is not null);
    }

    static bool IsRefOrPointer(ITypeSymbol type) => type.IsRefLikeType || type.TypeKind is TypeKind.Pointer or TypeKind.FunctionPointer;

    static bool IsExpressionTree(ITypeSymbol? type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.ToDisplayString() == "System.Linq.Expressions.LambdaExpression")
            {
                return true;
            }
        }

        return false;
    }

    static bool IsUnsafeOrRefOnly(SyntaxNode node)
        => node is StackAllocArrayCreationExpressionSyntax or ImplicitStackAllocArrayCreationExpressionSyntax or FixedStatementSyntax or UnsafeStatementSyntax or PointerTypeSyntax or FunctionPointerTypeSyntax
            || node.IsKind(SyntaxKind.AddressOfExpression)
            || node.IsKind(SyntaxKind.PointerIndirectionExpression)
            || node.IsKind(SyntaxKind.PointerMemberAccessExpression)
            || node.IsKind(SyntaxKind.MakeRefExpression)
            || node.IsKind(SyntaxKind.RefTypeExpression)
            || node.IsKind(SyntaxKind.RefValueExpression)
            || node.IsKind(SyntaxKind.ArgListExpression);

    static bool IsAwait(SyntaxNode node) => node switch
    {
        AwaitExpressionSyntax => true,
        CommonForEachStatementSyntax forEach => forEach.AwaitKeyword != default,
        UsingStatementSyntax usingStatement => usingStatement.AwaitKeyword != default,
        LocalDeclarationStatementSyntax declaration => declaration.AwaitKeyword != default,
        _ => false,
    };

    public static bool IsFunction(SyntaxNode node) => node is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax or MemberDeclarationSyntax or AccessorDeclarationSyntax;

    static string Truncate(SyntaxNode node)
    {
        var text = node.ToString().Trim();
        return text.Length <= 60 ? text : text[..60] + "...";
    }

    static RefusalReason Refuse(string code, string message, SyntaxNode at, string? suggestion)
        => new(code, message, Ranges.FromSpan(at.SyntaxTree, at.Span), suggestion);

    static RefusalReason Refuse(string code, string message, SyntaxTree tree, Microsoft.CodeAnalysis.Text.TextSpan span)
        => new(code, message, Ranges.FromSpan(tree, span), null);
}
