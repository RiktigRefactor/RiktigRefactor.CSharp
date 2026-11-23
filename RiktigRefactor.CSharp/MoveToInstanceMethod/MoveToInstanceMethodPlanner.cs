using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using RiktigRefactor.CSharp.Analysis;
using RiktigRefactor.CSharp.Protocol;
using RiktigRefactor.CSharp.Rewriting;
using RiktigRefactor.CSharp.Verification;

namespace RiktigRefactor.CSharp.MoveToInstanceMethod;

/// <summary>
/// Plans Move static method to instance method: the method moves into its receiver parameter's type, the parameter becomes <c>this</c>,
/// and every call <c>S.M(a, recv, b)</c> becomes <c>recv.M(a, b)</c>.
/// With <c>checkPreconditions</c> off it writes what it can and skips every check, for the naive implementation the tests compare against.
/// </summary>
public class MoveToInstanceMethodPlanner
{
    readonly Solution _solution;
    readonly IMethodSymbol _method;
    readonly IParameterSymbol _receiver;
    readonly INamedTypeSymbol _receiverType;
    readonly MethodDeclarationSyntax _declaration;
    readonly DocumentId _declarationDocument;
    readonly TypeDeclarationSyntax _targetDeclaration;
    readonly bool _checkPreconditions;
    readonly CancellationToken _cancellationToken;
    readonly List<SourceEdit> _edits = [];
    readonly List<(DocumentId Document, TextSpan Span)> _deletedSpans = [];
    readonly HashSet<TextSpan> _receiverParameterPassedAsReceiver = [];

    MoveToInstanceMethodPlanner(Solution solution, IMethodSymbol method, IParameterSymbol receiver, MethodDeclarationSyntax declaration, TypeDeclarationSyntax targetDeclaration, bool checkPreconditions, CancellationToken cancellationToken)
    {
        this._solution = solution;
        this._method = method;
        this._receiver = receiver;
        this._receiverType = (INamedTypeSymbol)receiver.Type;
        this._declaration = declaration;
        this._declarationDocument = solution.GetDocumentId(declaration.SyntaxTree)!;
        this._targetDeclaration = targetDeclaration;
        this._checkPreconditions = checkPreconditions;
        this._cancellationToken = cancellationToken;
    }

    public static async Task<(MoveToInstanceMethodPlan? Plan, RefusalReason? Refusal)> PlanAsync(
        Solution solution,
        DocumentId documentId,
        int position,
        string receiverName,
        bool checkPreconditions,
        CancellationToken cancellationToken)
    {
        var document = solution.GetDocument(documentId) ?? throw new ArgumentException("Document not in solution.", nameof(documentId));
        var method = await MethodTargets.FindMethodAtAsync(document, position, cancellationToken).ConfigureAwait(false);
        if (method is null)
        {
            return (null, Refuse(RefusalCodes.NoSymbol, "There is no method at the cursor.", null, "Put the cursor on the method's name."));
        }

        if (FindUnsupportedMethod(method, cancellationToken) is { } unsupported)
        {
            return (null, unsupported);
        }

        var receiver = method.Parameters.FirstOrDefault(parameter => parameter.Name == (receiverName.StartsWith('@') ? receiverName[1..] : receiverName));
        if (receiver is null)
        {
            return (null, Refuse(RefusalCodes.InvalidReceiver, $"`{method.Name}` has no parameter named `{receiverName}`.", method.Locations[0], null));
        }

        var targetDeclaration = await FindTargetDeclarationAsync(solution, document.Project, receiver, cancellationToken).ConfigureAwait(false);
        if (checkPreconditions && FindReceiverParameterProblem(receiver, targetDeclaration) is { } receiverProblem)
        {
            return (null, receiverProblem);
        }

        if (targetDeclaration is null)
        {
            return (null, Refuse(RefusalCodes.InvalidReceiver, $"The type of `{receiver.Name}` is not a class declared in this project.", receiver.Locations[0], null));
        }

        var declaration = (MethodDeclarationSyntax)method.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
        var planner = new MoveToInstanceMethodPlanner(solution, method, receiver, declaration, targetDeclaration, checkPreconditions, cancellationToken);
        var refusal = await planner.PlanAsync().ConfigureAwait(false);
        return refusal is null ? (new MoveToInstanceMethodPlan(method, planner._edits, planner._deletedSpans), null) : (null, refusal);
    }

    static RefusalReason? FindUnsupportedMethod(IMethodSymbol method, CancellationToken cancellationToken)
    {
        var isSupported = method is { MethodKind: MethodKind.Ordinary, IsStatic: true, IsExtensionMethod: false, IsExtern: false, IsGenericMethod: false, IsPartialDefinition: false, PartialImplementationPart: null, PartialDefinitionPart: null, IsAbstract: false, IsVirtual: false }
            && method.DeclaringSyntaxReferences is [var reference]
            && reference.GetSyntax(cancellationToken) is MethodDeclarationSyntax { Parent: TypeDeclarationSyntax } declaration
            && (declaration.Body is not null || declaration.ExpressionBody is not null);
        if (!isSupported)
        {
            return Refuse(RefusalCodes.NotStaticMethod, $"`{method.Name}` is not a non-generic static method with a body, declared once in a type of the workspace (extension methods are not supported).", method.Locations.FirstOrDefault(), null);
        }

        return MoveGenerics.IsGenericOrInGeneric(method.ContainingType)
            ? Refuse(RefusalCodes.GenericType, "The method's type is generic, or nested in a generic type.", method.Locations[0], null)
            : null;
    }

    static async Task<TypeDeclarationSyntax?> FindTargetDeclarationAsync(Solution solution, Project project, IParameterSymbol receiver, CancellationToken cancellationToken)
    {
        if (receiver.Type is not INamedTypeSymbol { TypeKind: TypeKind.Class or TypeKind.Struct } type)
        {
            return null;
        }

        foreach (var reference in type.DeclaringSyntaxReferences)
        {
            if (await reference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false) is TypeDeclarationSyntax { OpenBraceToken.RawKind: not 0 } declaration
                && SymbolIdentity.RegularDocumentId(solution, declaration.SyntaxTree) is { } documentId
                && documentId.ProjectId == project.Id)
            {
                return declaration;
            }
        }

        return null;
    }

    static RefusalReason? FindReceiverParameterProblem(IParameterSymbol receiver, TypeDeclarationSyntax? targetDeclaration)
    {
        var location = receiver.Locations[0];
        if (receiver.RefKind != RefKind.None || receiver.IsParams || receiver.HasExplicitDefaultValue || receiver.ScopedKind != ScopedKind.None)
        {
            return Refuse(RefusalCodes.InvalidReceiver, $"`{receiver.Name}` is `ref`, `out`, `in`, `params`, `scoped` or optional, so it can't become the receiver.", location, null);
        }

        if (receiver.Type is INamedTypeSymbol { TypeKind: TypeKind.Struct })
        {
            return Refuse(RefusalCodes.UnsupportedReceiver, $"`{receiver.Name}` is a struct, which `this` would alias instead of copy.", location, null);
        }

        if (receiver.Type is not INamedTypeSymbol { TypeKind: TypeKind.Class, IsStatic: false } type || targetDeclaration is null)
        {
            return Refuse(RefusalCodes.InvalidReceiver, $"The type of `{receiver.Name}` is not a class declared in this project.", location, null);
        }

        return MoveGenerics.IsGenericOrInGeneric(type)
            ? Refuse(RefusalCodes.GenericType, $"The type of `{receiver.Name}` is generic, or nested in a generic type.", location, null)
            : null;
    }

    async Task<RefusalReason?> PlanAsync()
    {
        if (this._checkPreconditions && await FindProblemAsync().ConfigureAwait(false) is { } problem)
        {
            return problem;
        }

        var callProblem = await PlanReferencesAsync().ConfigureAwait(false);
        if (callProblem is not null)
        {
            return callProblem;
        }

        var model = (await this._solution.GetDocument(this._declarationDocument)!.GetSemanticModelAsync(this._cancellationToken).ConfigureAwait(false))!;
        var bodyProblem = PlanBody(model);
        if (bodyProblem is not null)
        {
            return bodyProblem;
        }

        PlanSignature();
        this._edits.AddRange(SourceScopeNames.Qualify(model, this._declarationDocument, this._declaration, this._method.ContainingType, IsRewrittenElsewhere, this._cancellationToken));
        this._edits.AddRange(await DeclarationMove.PlanAsync(this._solution, this._declaration, this._targetDeclaration, this._cancellationToken).ConfigureAwait(false));
        return null;
    }

    bool IsRewrittenElsewhere(ISymbol symbol)
        => SymbolEqualityComparer.Default.Equals(SymbolIdentity.Normalize(symbol), this._method) || SymbolEqualityComparer.Default.Equals(symbol, this._receiver);

    async Task<RefusalReason?> FindProblemAsync()
    {
        var location = this._method.Locations[0];
        if (MethodTargets.PatternNames.Contains(this._method.Name))
        {
            return Refuse(RefusalCodes.ImplicitReference, $"`{this._method.Name}` is a name the compiler or runtime can look up implicitly on `{this._receiverType.Name}`.", location, null);
        }

        if (await FindMemberWithSameNameInHierarchyAsync().ConfigureAwait(false) is { } existing)
        {
            return Refuse(RefusalCodes.NameCollision, $"`{existing.ContainingType.Name}` in the hierarchy of `{this._receiverType.Name}` already has a member named `{this._method.Name}`, which the new method would hide, override, or collide with.", location, "Rename the method first.");
        }

        return await MethodTargets.FindDisabledTextMentioningAsync(this._solution, this._method.Name, this._cancellationToken).ConfigureAwait(false) is { } disabled
            ? Refuse(RefusalCodes.PreprocessorRegion, $"A conditionally compiled region mentions `{this._method.Name}`, so its calls can't be updated in every configuration.", disabled, null)
            : null;
    }

    async Task<ISymbol?> FindMemberWithSameNameInHierarchyAsync()
    {
        for (var type = this._receiverType; type is not null; type = type.BaseType)
        {
            if (type.GetMembers(this._method.Name).FirstOrDefault(member => !SymbolEqualityComparer.Default.Equals(member, this._method)) is { } member)
            {
                return member;
            }
        }

        var derived = await Microsoft.CodeAnalysis.FindSymbols.SymbolFinder.FindDerivedClassesAsync(this._receiverType, this._solution, transitive: true, cancellationToken: this._cancellationToken).ConfigureAwait(false);
        return derived.SelectMany(type => type.GetMembers(this._method.Name)).FirstOrDefault();
    }

    /// <summary>Every use of the receiver parameter in the body becomes <c>this</c>.</summary>
    RefusalReason? PlanBody(SemanticModel model)
    {
        SyntaxNode body = (SyntaxNode?)this._declaration.Body ?? this._declaration.ExpressionBody!;
        foreach (var name in body.DescendantNodes(descendIntoTrivia: true).OfType<IdentifierNameSyntax>())
        {
            if (name.Identifier.ValueText != this._receiver.Name
                || !SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(name, this._cancellationToken).Symbol, this._receiver)
                || this._receiverParameterPassedAsReceiver.Contains(name.Span))
            {
                continue;
            }

            if (this._checkPreconditions && (Independence.IsWrittenPosition(name) || IsInsideNameof(model, name, this._cancellationToken)))
            {
                return Refuse(RefusalCodes.ReceiverWritten, $"The body assigns `{this._receiver.Name}`, passes it by reference, or names it with `nameof`, which `this` can't be.", name.GetLocation(), null);
            }

            this._deletedSpans.Add((this._declarationDocument, name.Span));
            this._edits.Add(SourceEdit.Replace(this._declarationDocument, name.Span, new LiteralPiece("this")));
        }

        return null;
    }

    /// <summary>Removes <c>static</c> and the receiver parameter.</summary>
    void PlanSignature()
    {
        var staticModifier = this._declaration.Modifiers.First(modifier => modifier.IsKind(SyntaxKind.StaticKeyword));
        this._edits.Add(SourceEdit.Delete(this._declarationDocument, TextSpan.FromBounds(staticModifier.SpanStart, staticModifier.FullSpan.End)));

        var deleted = WithSeparator(this._declaration.ParameterList.Parameters, this._receiver.Ordinal);
        this._deletedSpans.Add((this._declarationDocument, deleted));
        this._edits.Add(SourceEdit.Delete(this._declarationDocument, deleted));
    }

    async Task<RefusalReason?> PlanReferencesAsync()
    {
        foreach (var location in await MethodTargets.FindReferenceLocationsAsync(this._solution, this._method, this._cancellationToken).ConfigureAwait(false))
        {
            var document = this._solution.GetDocument(location.SourceTree)!;
            var root = await document.GetSyntaxRootAsync(this._cancellationToken).ConfigureAwait(false);
            var model = (await document.GetSemanticModelAsync(this._cancellationToken).ConfigureAwait(false))!;
            var token = root!.FindToken(location.SourceSpan.Start, findInsideTrivia: true);
            if (token.Parent is not SimpleNameSyntax name || name.Identifier.ValueText != this._method.Name)
            {
                if (this._checkPreconditions)
                {
                    return Refuse(RefusalCodes.ImplicitReference, $"`{this._method.Name}` is used implicitly here, and the use can't be rewritten.", location, null);
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
        if (name.Ancestors().Any(ancestor => ancestor is CrefSyntax) || IsInsideNameof(model, name, this._cancellationToken))
        {
            PlanRequalifiedName(documentId, model, name);
            return null;
        }

        if (this._checkPreconditions && ExpressionTrees.IsInsideExpressionTreeOrQuery(model, name, this._cancellationToken))
        {
            return Refuse(RefusalCodes.ExpressionTreeOrQuery, $"`{this._method.Name}` is called inside an expression tree or a query expression, whose shape would change.", name.GetLocation(), null);
        }

        var call = name.Parent switch
        {
            InvocationExpressionSyntax invocation when invocation.Expression == name => invocation,
            MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax invocation } memberAccess when memberAccess.Name == name && invocation.Expression == memberAccess => invocation,
            _ => null,
        };
        if (call is null)
        {
            return this._checkPreconditions
                ? Refuse(RefusalCodes.MethodGroupReference, $"`{this._method.Name}` is used as a method group; there is no method group for an instance method that takes its receiver as an argument.", name.GetLocation(), "Write the delegate as a lambda that calls the method first.")
                : null;
        }

        if ((model.GetOperation(call, this._cancellationToken) as IInvocationOperation)?.Arguments
            .FirstOrDefault(argument => argument.Parameter?.Ordinal == this._receiver.Ordinal)?.Syntax is not ArgumentSyntax receiverArgument)
        {
            return Refuse(RefusalCodes.InvalidReceiver, "The call does not pass the receiver as an argument of its own.", call.GetLocation(), null);
        }

        if (this._checkPreconditions && FindCallProblem(model, call, receiverArgument) is { } callProblem)
        {
            return callProblem;
        }

        var receiver = receiverArgument.Expression;
        var deleted = WithSeparator(call.ArgumentList.Arguments, call.ArgumentList.Arguments.IndexOf(receiverArgument));
        var replaced = TextSpan.FromBounds(call.Expression.SpanStart, call.ArgumentList.OpenParenToken.Span.End);
        var isReceiverParameter = name.SyntaxTree == this._declaration.SyntaxTree
            && this._declaration.Span.Contains(receiver.Span)
            && receiver is IdentifierNameSyntax
            && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(receiver, this._cancellationToken).Symbol, this._receiver);
        if (isReceiverParameter)
        {
            this._receiverParameterPassedAsReceiver.Add(receiver.Span);
        }

        var needsParentheses = receiver is not (IdentifierNameSyntax or MemberAccessExpressionSyntax or InvocationExpressionSyntax or ElementAccessExpressionSyntax or ThisExpressionSyntax or ParenthesizedExpressionSyntax or ObjectCreationExpressionSyntax);
        SourcePiece[] prefix =
        [
            new LiteralPiece(needsParentheses ? "(" : string.Empty),
            isReceiverParameter ? new LiteralPiece("this") : new CopyPiece(documentId, receiver.Span),
            new LiteralPiece(needsParentheses ? ")." : "."),
            new NamePiece(this._method.Name, new BindsToOriginal(this._method)),
            new LiteralPiece("("),
        ];
        this._deletedSpans.Add((documentId, replaced));
        this._deletedSpans.Add((documentId, deleted));
        this._edits.Add(SourceEdit.Replace(documentId, replaced, prefix));
        this._edits.Add(SourceEdit.Delete(documentId, deleted));
        return null;
    }

    RefusalReason? FindCallProblem(SemanticModel model, InvocationExpressionSyntax call, ArgumentSyntax receiverArgument)
    {
        var receiver = receiverArgument.Expression;
        var receiverType = model.GetTypeInfo(receiver, this._cancellationToken).Type;
        if (!IsSameOrDerivedClass(receiverType, this._receiverType))
        {
            return Refuse(RefusalCodes.UnsupportedReceiver, $"The receiver `{receiver}` has type `{receiverType}`, so passing it needed a conversion that a receiver would not do.", receiver.GetLocation(), null);
        }

        var isTheReceiverParameterInsideTheMethod = receiver is IdentifierNameSyntax
            && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(receiver, this._cancellationToken).Symbol, this._receiver);
        if (!isTheReceiverParameterInsideTheMethod && !NonNullExpressions.IsProvablyNonNull(model, receiver, this._cancellationToken))
        {
            return Refuse(RefusalCodes.ReceiverMayBeNull, $"The receiver `{receiver}` might be null. The static method runs its body with a null argument, but an instance call would throw first.", receiver.GetLocation(), "Make it an extension method instead, which keeps the null behavior.");
        }

        foreach (var earlier in call.ArgumentList.Arguments.TakeWhile(argument => argument != receiverArgument))
        {
            if (!Independence.AreIndependent(model, earlier.Expression, receiver, this._cancellationToken))
            {
                return Refuse(RefusalCodes.EvaluationOrder, $"`{earlier.Expression}` is evaluated before the receiver `{receiver}` today, and would be evaluated after it.", earlier.GetLocation(), "Put the value in a local first.");
            }
        }

        return null;
    }

    /// <summary>The span of the item at <paramref name="index"/>, with the comma that separates it from the next item, or else from the previous one.</summary>
    static TextSpan WithSeparator<TNode>(SeparatedSyntaxList<TNode> list, int index)
        where TNode : SyntaxNode
    {
        if (list.Count == 1)
        {
            return list[index].Span;
        }

        if (index < list.Count - 1)
        {
            return TextSpan.FromBounds(list[index].SpanStart, list[index + 1].SpanStart);
        }

        return TextSpan.FromBounds(list[index - 1].Span.End, list[index].Span.End);
    }

    static bool IsSameOrDerivedClass(ITypeSymbol? type, INamedTypeSymbol target)
    {
        for (var current = type as INamedTypeSymbol; current is not null; current = current.BaseType)
        {
            if (current.TypeKind == TypeKind.Class && SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, target.OriginalDefinition))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>A name in <c>nameof</c> or a cref keeps naming the method, now through the receiver's type.</summary>
    void PlanRequalifiedName(DocumentId documentId, SemanticModel model, SimpleNameSyntax name)
    {
        var type = new TypePiece(this._receiverType.ToMinimalDisplayString(model, name.SpanStart), this._receiverType);
        var qualifier = name.Parent switch
        {
            MemberAccessExpressionSyntax memberAccess when memberAccess.Name == name => memberAccess.Expression,
            QualifiedNameSyntax qualified when qualified.Right == name => qualified.Left,
            NameMemberCrefSyntax { Parent: QualifiedCrefSyntax qualifiedCref } => qualifiedCref.Container,
            _ => null,
        };
        if (qualifier is null)
        {
            this._edits.Add(SourceEdit.Insert(documentId, name.SpanStart, type, new LiteralPiece(".")));
            return;
        }

        this._deletedSpans.Add((documentId, qualifier.Span));
        this._edits.Add(SourceEdit.Replace(documentId, qualifier.Span, type));
    }

    static bool IsInsideNameof(SemanticModel model, SyntaxNode node, CancellationToken cancellationToken)
        => node.Ancestors().OfType<InvocationExpressionSyntax>().Any(invocation =>
            invocation.Expression is IdentifierNameSyntax { Identifier.ValueText: "nameof" }
            && model.GetOperation(invocation, cancellationToken) is INameOfOperation);

    static RefusalReason Refuse(string code, string message, Location? location, string? suggestion)
        => new(code, message, location is null ? null : Ranges.FromLocation(location), suggestion);
}
