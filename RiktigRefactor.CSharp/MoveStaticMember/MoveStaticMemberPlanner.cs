using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;
using RiktigRefactor.CSharp.Analysis;
using RiktigRefactor.CSharp.Protocol;
using RiktigRefactor.CSharp.Rewriting;
using RiktigRefactor.CSharp.Verification;

namespace RiktigRefactor.CSharp.MoveStaticMember;

/// <summary>
/// Plans Move static member: finds the member and the target type, checks the preconditions, and writes the edits that move the declaration and update every reference.
/// With <c>checkPreconditions</c> off it writes what it can and skips every check, for the naive implementation the tests compare against.
/// </summary>
public class MoveStaticMemberPlanner
{
    const int MethodImplSynchronized = 0x20;

    readonly Solution _solution;
    readonly ISymbol _member;
    readonly INamedTypeSymbol _source;
    readonly INamedTypeSymbol _target;
    readonly MemberDeclarationSyntax _declaration;
    readonly DocumentId _sourceDocument;
    readonly TypeDeclarationSyntax _targetDeclaration;
    readonly bool _checkPreconditions;
    readonly CancellationToken _cancellationToken;
    readonly List<SourceEdit> _edits = [];
    readonly List<(DocumentId Document, TextSpan Span)> _replacedQualifiers = [];
    Compilation? _compilation;

    MoveStaticMemberPlanner(Solution solution, ISymbol member, MemberDeclarationSyntax declaration, INamedTypeSymbol target, TypeDeclarationSyntax targetDeclaration, bool checkPreconditions, CancellationToken cancellationToken)
    {
        this._solution = solution;
        this._member = member;
        this._source = member.ContainingType;
        this._target = target;
        this._declaration = declaration;
        this._sourceDocument = solution.GetDocumentId(declaration.SyntaxTree)!;
        this._targetDeclaration = targetDeclaration;
        this._checkPreconditions = checkPreconditions;
        this._cancellationToken = cancellationToken;
    }

    public static async Task<(MoveStaticMemberPlan? Plan, RefusalReason? Refusal)> PlanAsync(
        Solution solution,
        DocumentId documentId,
        int position,
        string target,
        bool checkPreconditions,
        CancellationToken cancellationToken)
    {
        var document = solution.GetDocument(documentId) ?? throw new ArgumentException("Document not in solution.", nameof(documentId));
        var found = await SymbolFinder.FindSymbolAtPositionAsync(document, position, cancellationToken).ConfigureAwait(false);
        if (found is null or INamespaceOrTypeSymbol or ILocalSymbol or IParameterSymbol or IRangeVariableSymbol or ILabelSymbol)
        {
            return (null, Refuse(RefusalCodes.NoSymbol, "There is no member at the cursor.", null, "Put the cursor on the member's name."));
        }

        var member = SymbolIdentity.Normalize(found);
        if (FindUnmovableMember(member, cancellationToken) is { } unmovable)
        {
            return (null, unmovable);
        }

        var declaration = MovedDeclaration(member, cancellationToken)!;
        var sourceProject = solution.GetDocument(declaration.SyntaxTree)!.Project;
        var targetType = await FindTargetAsync(sourceProject, target, cancellationToken).ConfigureAwait(false);
        if (targetType is null)
        {
            return (null, Refuse(RefusalCodes.InvalidTarget, $"`{target}` is not a class, struct or record with a body declared in the same project as the member.", null, "Write the target's namespace-qualified name, such as `Shop.Pricing`."));
        }

        var targetDeclaration = targetType.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax(cancellationToken))
            .OfType<TypeDeclarationSyntax>()
            .First(syntax => SymbolIdentity.RegularDocumentId(solution, syntax.SyntaxTree) is not null);
        var planner = new MoveStaticMemberPlanner(solution, member, declaration, targetType, targetDeclaration, checkPreconditions, cancellationToken);
        var refusal = await planner.PlanAsync().ConfigureAwait(false);
        return refusal is null ? (new MoveStaticMemberPlan(member, planner._edits, planner._replacedQualifiers), null) : (null, refusal);
    }

    static RefusalReason? FindUnmovableMember(ISymbol member, CancellationToken cancellationToken)
    {
        var location = member.Locations.FirstOrDefault();
        if (member is IMethodSymbol { IsStatic: false } or IPropertySymbol { IsStatic: false } or IEventSymbol { IsStatic: false } or IFieldSymbol { IsStatic: false, IsConst: false })
        {
            return Refuse(RefusalCodes.NotMovable, $"`{member.Name}` is an instance member, which needs an instance.", location, "Use Make method static first.");
        }

        var isSupportedKind = member is IMethodSymbol { MethodKind: MethodKind.Ordinary, IsExtern: false, IsPartialDefinition: false, PartialImplementationPart: null, PartialDefinitionPart: null }
            or IFieldSymbol or IPropertySymbol { IsIndexer: false } or IEventSymbol;
        var isInSupportedType = member.ContainingType is { TypeKind: TypeKind.Class or TypeKind.Struct, IsImplicitlyDeclared: false };
        if (!isSupportedKind || !isInSupportedType || member.IsAbstract || member.IsVirtual || member.IsOverride || MovedDeclaration(member, cancellationToken) is null)
        {
            return Refuse(RefusalCodes.NotMovable, $"`{member.Name}` is not a static method, field, property, event or constant declared once, with its own declaration, in a class, struct or record.", location, null);
        }

        return null;
    }

    /// <summary>The declaration to move; a field must be the only variable its declaration declares.</summary>
    static MemberDeclarationSyntax? MovedDeclaration(ISymbol member, CancellationToken cancellationToken)
    {
        if (member.DeclaringSyntaxReferences is not [var reference])
        {
            return null;
        }

        return reference.GetSyntax(cancellationToken) switch
        {
            VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Variables.Count: 1, Parent: BaseFieldDeclarationSyntax field } } => field,
            MethodDeclarationSyntax method when method.Body is not null || method.ExpressionBody is not null => method,
            PropertyDeclarationSyntax property => property,
            EventDeclarationSyntax eventDeclaration => eventDeclaration,
            _ => null,
        };
    }

    static async Task<INamedTypeSymbol?> FindTargetAsync(Project project, string target, CancellationToken cancellationToken)
    {
        var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        var unqualified = target.StartsWith("global::", StringComparison.Ordinal) ? target["global::".Length..] : target;
        return MethodTargets.AllTypes(compilation!.Assembly.GlobalNamespace)
            .Where(type => type.TypeKind is TypeKind.Class or TypeKind.Struct && type.ToDisplayString() == unqualified)
            .FirstOrDefault(type => type.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax(cancellationToken) is TypeDeclarationSyntax { OpenBraceToken.RawKind: not 0 }));
    }

    async Task<RefusalReason?> PlanAsync()
    {
        if (this._checkPreconditions && await FindProblemAsync().ConfigureAwait(false) is { } problem)
        {
            return problem;
        }

        var referenceProblem = await PlanReferencesAsync().ConfigureAwait(false);
        if (referenceProblem is not null)
        {
            return referenceProblem;
        }

        await PlanQualifiedNamesInDeclarationAsync().ConfigureAwait(false);
        await PlanMoveAsync().ConfigureAwait(false);
        return null;
    }

    async Task<RefusalReason?> FindProblemAsync()
    {
        this._compilation = await this._solution.GetDocument(this._sourceDocument)!.Project.GetCompilationAsync(this._cancellationToken).ConfigureAwait(false);
        var location = this._member.Locations[0];
        if (SymbolEqualityComparer.Default.Equals(this._source, this._target))
        {
            return Refuse(RefusalCodes.InvalidTarget, $"`{this._member.Name}` is already in `{this._target.Name}`.", location, null);
        }

        if (IsGenericOrInGeneric(this._source) || IsGenericOrInGeneric(this._target))
        {
            return Refuse(RefusalCodes.GenericType, "The member's type or the target is generic, or nested in a generic type; each constructed type has its own static state and type parameters.", location, null);
        }

        if (this._member is IMethodSymbol { IsExtensionMethod: true } && !this._target.IsStatic)
        {
            return Refuse(RefusalCodes.InvalidTarget, "An extension method can only move to a static class.", location, null);
        }

        if (this._member is IMethodSymbol { IsExtensionMethod: true } && (this._target.ContainingType is not null || MethodTargets.PatternNames.Contains(this._member.Name)))
        {
            return Refuse(RefusalCodes.InvalidTarget, "An extension method can only move to a top-level static class, and not when its name is looked up by pattern.", location, null);
        }

        if (this._member.Name == "Main" && this._member is IMethodSymbol)
        {
            return Refuse(RefusalCodes.NotMovable, "`Main` may be the program's entry point, which is found by its containing type.", location, null);
        }

        return FindNameCollision()
            ?? FindLockOnType()
            ?? FindInitializationProblem()
            ?? FindPreprocessorDirectiveInDeclaration()
            ?? await FindDisabledTextAsync(this._solution, this._member.Name, this._cancellationToken).ConfigureAwait(false);
    }

    static bool IsGenericOrInGeneric(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            if (current.IsGenericType)
            {
                return true;
            }
        }

        return false;
    }

    RefusalReason? FindNameCollision()
    {
        var location = this._member.Locations[0];
        var existing = this._target.GetMembers(this._member.Name).Cast<ISymbol>().Concat(this._target.GetTypeMembers(this._member.Name));
        if (existing.Any(other => this._member is not IMethodSymbol || other is not IMethodSymbol))
        {
            return Refuse(RefusalCodes.NameCollision, $"`{this._target.Name}` already has a member named `{this._member.Name}`.", location, "Rename the member first.");
        }

        for (var baseType = this._target.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if (!baseType.GetMembers(this._member.Name).IsEmpty || !baseType.GetTypeMembers(this._member.Name).IsEmpty)
            {
                return Refuse(RefusalCodes.NameCollision, $"`{this._member.Name}` would hide a member of `{baseType.Name}`, a base type of the target.", location, "Rename the member first.");
            }
        }

        return null;
    }

    RefusalReason? FindLockOnType()
    {
        var isSynchronized = this._member.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.ToDisplayString() == "System.Runtime.CompilerServices.MethodImplAttribute"
            && attribute.ConstructorArguments is [{ Value: int options }, ..]
            && (options & MethodImplSynchronized) != 0);
        var locksType = this._declaration.DescendantNodes().OfType<LockStatementSyntax>().Any(statement => statement.Expression is TypeOfExpressionSyntax);
        return isSynchronized || locksType
            ? Refuse(RefusalCodes.LockOnType, "The member locks on its type object, which would change.", this._member.Locations[0], null)
            : null;
    }

    /// <summary>
    /// When a type's static state is initialized is observable, so the member's own initializer must be a constant,
    /// and neither type may have a static constructor or a static initializer that isn't a constant.
    /// </summary>
    RefusalReason? FindInitializationProblem()
    {
        if (this._member is IFieldSymbol { IsConst: true })
        {
            return null;
        }

        if (InitializerOf(this._declaration) is { } initializer && !IsConstant(initializer))
        {
            return Refuse(RefusalCodes.InitializationOrder, $"`{this._member.Name}` has an initializer that is not a constant, and it would run at a different time.", initializer.GetLocation(), null);
        }

        foreach (var type in new[] { this._source, this._target })
        {
            if (FindNonConstantStaticInitialization(type) is { } initialization)
            {
                return Refuse(RefusalCodes.InitializationOrder, $"`{type.Name}` has a static constructor or a static initializer that is not a constant, so using the member there would run it at a different time.", initialization, null);
            }
        }

        return null;
    }

    static ExpressionSyntax? InitializerOf(MemberDeclarationSyntax declaration) => declaration switch
    {
        BaseFieldDeclarationSyntax field => field.Declaration.Variables[0].Initializer?.Value,
        PropertyDeclarationSyntax property => property.Initializer?.Value,
        _ => null,
    };

    Location? FindNonConstantStaticInitialization(INamedTypeSymbol type)
    {
        if (type.StaticConstructors.FirstOrDefault(constructor => !constructor.IsImplicitlyDeclared) is { } constructor)
        {
            return constructor.Locations[0];
        }

        foreach (var member in type.GetMembers().Where(member => member.IsStatic && !SymbolEqualityComparer.Default.Equals(member, this._member)))
        {
            foreach (var reference in member.DeclaringSyntaxReferences)
            {
                var initializer = reference.GetSyntax(this._cancellationToken) switch
                {
                    VariableDeclaratorSyntax variable => variable.Initializer?.Value,
                    PropertyDeclarationSyntax property => property.Initializer?.Value,
                    _ => null,
                };
                if (initializer is not null && !IsConstant(initializer))
                {
                    return initializer.GetLocation();
                }
            }
        }

        return null;
    }

    bool IsConstant(ExpressionSyntax expression)
    {
        if (expression is LiteralExpressionSyntax or DefaultExpressionSyntax)
        {
            return true;
        }

        var model = this._compilation!.GetSemanticModel(expression.SyntaxTree);
        return model.GetConstantValue(expression, this._cancellationToken).HasValue;
    }

    RefusalReason? FindPreprocessorDirectiveInDeclaration()
        => this._declaration.DescendantTrivia(descendIntoTrivia: true).Any(trivia => trivia.IsDirective || trivia.IsKind(SyntaxKind.DisabledTextTrivia))
            || this._declaration.GetLeadingTrivia().Any(trivia => trivia.IsDirective)
            ? Refuse(RefusalCodes.PreprocessorRegion, "The member's declaration contains a preprocessor directive.", this._member.Locations[0], null)
            : null;

    static async Task<RefusalReason?> FindDisabledTextAsync(Solution solution, string name, CancellationToken cancellationToken)
        => await MethodTargets.FindDisabledTextMentioningAsync(solution, name, cancellationToken).ConfigureAwait(false) is { } disabled
            ? Refuse(RefusalCodes.PreprocessorRegion, $"A conditionally compiled region mentions `{name}`, so its references can't be updated in every configuration.", disabled, null)
            : null;

    async Task<RefusalReason?> PlanReferencesAsync()
    {
        foreach (var location in await MethodTargets.FindReferenceLocationsAsync(this._solution, this._member, this._cancellationToken).ConfigureAwait(false))
        {
            var document = this._solution.GetDocument(location.SourceTree)!;
            var root = await document.GetSyntaxRootAsync(this._cancellationToken).ConfigureAwait(false);
            var model = (await document.GetSemanticModelAsync(this._cancellationToken).ConfigureAwait(false))!;
            var token = root!.FindToken(location.SourceSpan.Start, findInsideTrivia: true);
            if (token.Parent is not SimpleNameSyntax name || name.Identifier.ValueText != this._member.Name)
            {
                if (this._checkPreconditions)
                {
                    return Refuse(RefusalCodes.ImplicitReference, $"`{this._member.Name}` is used implicitly here, and the use can't be rewritten.", location, null);
                }

                continue;
            }

            var problem = PlanReference(document.Id, model, name);
            if (problem is not null)
            {
                return problem;
            }
        }

        return null;
    }

    RefusalReason? PlanReference(DocumentId documentId, SemanticModel model, SimpleNameSyntax name)
    {
        if (model.GetSymbolInfo(name, this._cancellationToken).Symbol is IMethodSymbol { ReducedFrom: not null })
        {
            return null;
        }

        var qualifier = name.Parent switch
        {
            MemberAccessExpressionSyntax memberAccess when memberAccess.Name == name => memberAccess.Expression,
            QualifiedNameSyntax qualified when qualified.Right == name => qualified.Left,
            NameMemberCrefSyntax { Parent: QualifiedCrefSyntax qualifiedCref } => qualifiedCref.Container,
            _ => null,
        };
        if (qualifier is null)
        {
            Insert(documentId, name.SpanStart, TargetType(model, name.SpanStart), new LiteralPiece("."));
            return null;
        }

        if (this._checkPreconditions && model.GetSymbolInfo(qualifier, this._cancellationToken).Symbol is not INamedTypeSymbol)
        {
            return Refuse(RefusalCodes.NotMovable, $"`{qualifier}` is not a type name, so the reference can't be rewritten.", qualifier.GetLocation(), null);
        }

        this._replacedQualifiers.Add((documentId, qualifier.Span));
        Replace(documentId, qualifier.Span, TargetType(model, qualifier.SpanStart));
        return null;
    }

    async Task PlanQualifiedNamesInDeclarationAsync()
    {
        var model = (await this._solution.GetDocument(this._sourceDocument)!.GetSemanticModelAsync(this._cancellationToken).ConfigureAwait(false))!;
        this._edits.AddRange(SourceScopeNames.Qualify(model, this._sourceDocument, this._declaration, this._source, symbol => SymbolEqualityComparer.Default.Equals(SymbolIdentity.Normalize(symbol), this._member), this._cancellationToken));
    }

    async Task PlanMoveAsync()
        => this._edits.AddRange(await DeclarationMove.PlanAsync(this._solution, this._declaration, this._targetDeclaration, this._cancellationToken).ConfigureAwait(false));

    TypePiece TargetType(SemanticModel model, int position) => new(this._target.ToMinimalDisplayString(model, position), this._target);

    void Insert(DocumentId documentId, int position, params SourcePiece[] pieces)
        => Replace(documentId, new TextSpan(position, 0), pieces);

    void Replace(DocumentId documentId, TextSpan span, params SourcePiece[] pieces)
        => this._edits.Add(SourceEdit.Replace(documentId, span, pieces));

    static RefusalReason Refuse(string code, string message, Location? location, string? suggestion)
        => new(code, message, location is null ? null : Ranges.FromLocation(location), suggestion);
}
