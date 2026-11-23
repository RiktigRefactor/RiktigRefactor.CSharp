using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RiktigRefactor.CSharp.Protocol;
using RiktigRefactor.CSharp.Verification;

namespace RiktigRefactor.CSharp.Rename;

/// <summary>Checks that can refuse a rename before any edit is computed, because Roslyn would rename it incorrectly or incompletely.</summary>
public static class RenamePreconditions
{
    const string CallerArgumentExpressionAttribute = "System.Runtime.CompilerServices.CallerArgumentExpressionAttribute";

    static readonly SyntaxKind[] ForEachKinds = [SyntaxKind.ForEachStatement, SyntaxKind.ForEachVariableStatement, SyntaxKind.SpreadElement];
    static readonly SyntaxKind[] DisposeKinds = [SyntaxKind.UsingStatement, SyntaxKind.LocalDeclarationStatement, .. ForEachKinds];
    static readonly SyntaxKind[] AwaitKinds = [SyntaxKind.AwaitExpression, .. DisposeKinds];
    static readonly SyntaxKind[] RecordKinds = [SyntaxKind.RecordDeclaration, SyntaxKind.RecordStructDeclaration];
    static readonly SyntaxKind[] QueryKinds = [SyntaxKind.QueryExpression];
    static readonly SyntaxKind[] IndexingKinds = [SyntaxKind.SpreadElement, SyntaxKind.ListPattern, SyntaxKind.IndexExpression, SyntaxKind.RangeExpression, SyntaxKind.ElementAccessExpression, SyntaxKind.SlicePattern];
    static readonly SyntaxKind[] DeconstructKinds = [SyntaxKind.ForEachVariableStatement, SyntaxKind.RecursivePattern, SyntaxKind.SimpleAssignmentExpression, SyntaxKind.ParenthesizedVariableDesignation];
    static readonly SyntaxKind[] InterpolationKinds = [SyntaxKind.InterpolatedStringExpression];

    static readonly Dictionary<string, SyntaxKind[]> PatternNames = new(StringComparer.Ordinal)
    {
        ["Add"] = [SyntaxKind.CollectionInitializerExpression, SyntaxKind.CollectionExpression],
        ["GetEnumerator"] = ForEachKinds,
        ["GetAsyncEnumerator"] = ForEachKinds,
        ["MoveNext"] = ForEachKinds,
        ["MoveNextAsync"] = ForEachKinds,
        ["Current"] = ForEachKinds,
        ["Dispose"] = DisposeKinds,
        ["DisposeAsync"] = DisposeKinds,
        ["GetAwaiter"] = AwaitKinds,
        ["GetResult"] = AwaitKinds,
        ["IsCompleted"] = AwaitKinds,
        ["OnCompleted"] = AwaitKinds,
        ["UnsafeOnCompleted"] = AwaitKinds,
        ["Deconstruct"] = [.. DeconstructKinds, .. RecordKinds],
        ["Equals"] = RecordKinds,
        ["GetHashCode"] = RecordKinds,
        ["ToString"] = RecordKinds,
        ["PrintMembers"] = RecordKinds,
        ["EqualityContract"] = RecordKinds,
        ["Select"] = QueryKinds,
        ["SelectMany"] = QueryKinds,
        ["Where"] = QueryKinds,
        ["Join"] = QueryKinds,
        ["GroupJoin"] = QueryKinds,
        ["OrderBy"] = QueryKinds,
        ["OrderByDescending"] = QueryKinds,
        ["ThenBy"] = QueryKinds,
        ["ThenByDescending"] = QueryKinds,
        ["GroupBy"] = QueryKinds,
        ["Cast"] = QueryKinds,
        ["Length"] = IndexingKinds,
        ["Count"] = IndexingKinds,
        ["Slice"] = IndexingKinds,
        ["Substring"] = IndexingKinds,
        ["GetPinnableReference"] = [SyntaxKind.FixedStatement],
        ["AppendLiteral"] = InterpolationKinds,
        ["AppendFormatted"] = InterpolationKinds,
    };

    /// <summary>
    /// Refuses when the member overrides or implements a member declared outside the workspace.
    /// Renaming it would silently stop it from overriding or implementing that member.
    /// </summary>
    public static RefusalReason? FindExternalHierarchy(Solution solution, ISymbol symbol)
    {
        var external = RelatedMembers(symbol).FirstOrDefault(related => !IsDeclaredInWorkspace(solution, related));
        return external is null
            ? null
            : new RefusalReason(
                RefusalCodes.NotRenameable,
                $"`{symbol.ToDisplayString()}` overrides or implements `{external.ToDisplayString()}`, which is declared outside the workspace.",
                null,
                null);
    }

    /// <summary>
    /// Refuses when a parameter's name appears in a CallerArgumentExpression attribute string, which names the parameter
    /// by text and so is not updated by the rename.
    /// </summary>
    public static RefusalReason? FindParameterNamedInString(ISymbol symbol, string newName)
    {
        if (symbol is not IParameterSymbol { ContainingSymbol: IMethodSymbol method })
        {
            return null;
        }

        var namesParameter = method.Parameters
            .SelectMany(parameter => parameter.GetAttributes())
            .Where(attribute => attribute.AttributeClass?.ToDisplayString() == CallerArgumentExpressionAttribute)
            .Any(attribute => attribute.ConstructorArguments.FirstOrDefault().Value is string named && (named == symbol.Name || named == newName));
        return namesParameter
            ? new RefusalReason(
                RefusalCodes.StringNamesSymbol,
                $"A CallerArgumentExpression attribute names the parameter `{symbol.Name}` or `{newName}` in a string, which the rename would not update.",
                null,
                null)
            : null;
    }

    /// <summary>
    /// Refuses when a dynamically bound member access or named argument uses the old or new name.
    /// Those bind by name at run time, so the rename could break them, or make them start binding to the renamed member.
    /// </summary>
    public static async Task<RefusalReason?> FindDynamicAccessAsync(
        Solution solution,
        string oldName,
        string newName,
        CancellationToken cancellationToken)
    {
        foreach (var document in solution.Projects.SelectMany(project => project.Documents))
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var candidates = root?.DescendantNodes().Where(node => NamesUsedForBinding(node, oldName, newName)).ToList() ?? [];
            if (candidates.Count == 0)
            {
                continue;
            }

            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var dynamicUse = candidates.FirstOrDefault(node => IsBoundDynamically(model!, node, cancellationToken));
            if (dynamicUse is not null)
            {
                return new RefusalReason(
                    RefusalCodes.DynamicAccessPossible,
                    $"A dynamically bound expression uses `{oldName}` or `{newName}`, which binds by name at run time and can't be verified.",
                    Ranges.FromSpan(dynamicUse.SyntaxTree, dynamicUse.Span),
                    null);
            }
        }

        return null;
    }

    /// <summary>
    /// Refuses when a member is renamed from or to a name that the language looks up implicitly
    /// (collection initializers, foreach, await, deconstruction, query expressions, and the like) and the workspace uses that construct.
    /// Records count too: a user-declared Equals, GetHashCode, ToString, PrintMembers, or Deconstruct replaces the one the compiler would synthesize.
    /// Those lookups have no name in the source, so the binding verifier can't see them change.
    /// A construct whose lookup can't reach the renamed member (by the type it looks the name up on) is ignored; see <see cref="PatternLookupScope"/>.
    /// </summary>
    public static async Task<RefusalReason?> FindPatternBasedUseAsync(
        Solution solution,
        ISymbol symbol,
        string oldName,
        string newName,
        CancellationToken cancellationToken)
    {
        if (symbol.Kind is not (SymbolKind.Method or SymbolKind.Property or SymbolKind.Field or SymbolKind.Event))
        {
            return null;
        }

        var isPatternName = IsPatternName(oldName) || IsPatternName(newName);
        if (!isPatternName)
        {
            return null;
        }

        var renamedMemberTypes = PatternLookupScope.ContainingTypesOfRenamedMembers(symbol, RelatedMembers(symbol));
        foreach (var document in solution.Projects.SelectMany(project => project.Documents))
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            bool MayFindRenamedMember(SyntaxNode node, string name)
                => UsesPatternName(node, name) && PatternLookupScope.MayFind(model!, node, name, renamedMemberTypes, cancellationToken);

            var use = root?.DescendantNodes().FirstOrDefault(node => MayFindRenamedMember(node, oldName) || MayFindRenamedMember(node, newName));
            if (use is not null)
            {
                return new RefusalReason(
                    RefusalCodes.BindingWouldChange,
                    $"`{oldName}` or `{newName}` is looked up implicitly by a language construct, which can't be verified.",
                    Ranges.FromSpan(use.SyntaxTree, use.Span),
                    null);
            }
        }

        return null;
    }

    /// <summary>
    /// Refuses when source-generated code mentions the old or new name, or uses a construct that looks either up implicitly.
    /// The rename can't edit generated code, and the binding verifier only sees regular documents.
    /// </summary>
    public static async Task<RefusalReason?> FindGeneratedCodeUseAsync(
        Solution solution,
        string oldName,
        string newName,
        CancellationToken cancellationToken)
    {
        foreach (var project in solution.Projects)
        {
            foreach (var generated in await project.GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false))
            {
                var root = await generated.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var mentionsName = root?.DescendantTokens(descendIntoTrivia: true)
                    .Any(token => token.IsKind(SyntaxKind.IdentifierToken) && IsOneOf(token.ValueText, oldName, newName)) ?? false;
                var usesPatternName = root?.DescendantNodes().Any(node => UsesPatternName(node, oldName) || UsesPatternName(node, newName)) ?? false;
                if (mentionsName || usesPatternName)
                {
                    return new RefusalReason(
                        RefusalCodes.BindingWouldChange,
                        $"Source-generated code ({generated.HintName}) uses `{oldName}` or `{newName}`, and generated code can't be renamed or verified.",
                        null,
                        null);
                }
            }
        }

        return null;
    }

    /// <summary>Refuses when the rename changes what the source generators produce, since that code is outside the verified edits.</summary>
    public static async Task<RefusalReason?> FindGeneratedOutputChangeAsync(Solution oldSolution, Solution newSolution, CancellationToken cancellationToken)
    {
        foreach (var oldProject in oldSolution.Projects)
        {
            var newProject = newSolution.GetProject(oldProject.Id);
            var oldGenerated = await GeneratedTextsAsync(oldProject, cancellationToken).ConfigureAwait(false);
            var newGenerated = newProject is null ? [] : await GeneratedTextsAsync(newProject, cancellationToken).ConfigureAwait(false);
            var isUnchanged = oldGenerated.Count == newGenerated.Count
                && oldGenerated.All(pair => newGenerated.TryGetValue(pair.Key, out var text) && text == pair.Value);
            if (!isUnchanged)
            {
                return new RefusalReason(
                    RefusalCodes.BindingWouldChange,
                    $"The rename changes the source-generated code of project {oldProject.Name}, which can't be verified.",
                    null,
                    null);
            }
        }

        return null;
    }

    static async Task<Dictionary<string, string>> GeneratedTextsAsync(Project project, CancellationToken cancellationToken)
    {
        var texts = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var generated in await project.GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false))
        {
            texts[generated.FilePath ?? generated.HintName] = (await generated.GetTextAsync(cancellationToken).ConfigureAwait(false)).ToString();
        }

        return texts;
    }

    static bool IsPatternName(string name) => PatternNames.ContainsKey(name);

    static bool UsesPatternName(SyntaxNode node, string name)
        => PatternNames.TryGetValue(name, out var kinds) && kinds.Contains(node.Kind())
            && (node is not LocalDeclarationStatementSyntax declaration || declaration.UsingKeyword != default);

    static bool NamesUsedForBinding(SyntaxNode node, string oldName, string newName) => node switch
    {
        MemberAccessExpressionSyntax access => IsOneOf(access.Name.Identifier.ValueText, oldName, newName),
        MemberBindingExpressionSyntax binding => IsOneOf(binding.Name.Identifier.ValueText, oldName, newName),
        ArgumentSyntax { NameColon: { } nameColon } => IsOneOf(nameColon.Name.Identifier.ValueText, oldName, newName),
        _ => false,
    };

    static bool IsOneOf(string text, string first, string second) => text == first || text == second;

    static bool IsBoundDynamically(SemanticModel model, SyntaxNode node, CancellationToken cancellationToken)
    {
        return node switch
        {
            MemberAccessExpressionSyntax access => IsDynamic(model, access.Expression, cancellationToken),
            MemberBindingExpressionSyntax binding => IsDynamic(model, ConditionalReceiver(binding), cancellationToken),
            ArgumentSyntax { Parent: BaseArgumentListSyntax list } => list.Arguments.Any(argument => IsDynamic(model, argument.Expression, cancellationToken)),
            _ => false,
        };
    }

    static ExpressionSyntax? ConditionalReceiver(MemberBindingExpressionSyntax binding)
        => binding.Ancestors().OfType<ConditionalAccessExpressionSyntax>().FirstOrDefault()?.Expression;

    static bool IsDynamic(SemanticModel model, ExpressionSyntax? expression, CancellationToken cancellationToken)
        => expression is not null && model.GetTypeInfo(expression, cancellationToken).Type?.TypeKind == TypeKind.Dynamic;

    static IEnumerable<ISymbol> RelatedMembers(ISymbol symbol)
    {
        for (var overridden = OverriddenOf(symbol); overridden is not null; overridden = OverriddenOf(overridden))
        {
            yield return overridden;
        }

        foreach (var explicitImplementation in ExplicitImplementationsOf(symbol))
        {
            yield return explicitImplementation;
        }

        if (symbol.ContainingType is { } type && symbol.Kind is SymbolKind.Method or SymbolKind.Property or SymbolKind.Event)
        {
            foreach (var interfaceMember in type.AllInterfaces.SelectMany(@interface => @interface.GetMembers()))
            {
                if (SymbolEqualityComparer.Default.Equals(type.FindImplementationForInterfaceMember(interfaceMember), symbol))
                {
                    yield return interfaceMember;
                }
            }
        }
    }

    static ISymbol? OverriddenOf(ISymbol symbol) => symbol switch
    {
        IMethodSymbol method => method.OverriddenMethod,
        IPropertySymbol property => property.OverriddenProperty,
        IEventSymbol @event => @event.OverriddenEvent,
        _ => null,
    };

    static IEnumerable<ISymbol> ExplicitImplementationsOf(ISymbol symbol) => symbol switch
    {
        IMethodSymbol method => method.ExplicitInterfaceImplementations,
        IPropertySymbol property => property.ExplicitInterfaceImplementations,
        IEventSymbol @event => @event.ExplicitInterfaceImplementations,
        _ => [],
    };

    static bool IsDeclaredInWorkspace(Solution solution, ISymbol symbol)
        => symbol.Locations.Length > 0
            && symbol.Locations.All(location => location.IsInSource && solution.GetDocumentId(location.SourceTree) is not null);
}
