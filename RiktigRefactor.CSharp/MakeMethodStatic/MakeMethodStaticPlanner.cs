using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using RiktigRefactor.CSharp.Analysis;
using RiktigRefactor.CSharp.Protocol;
using RiktigRefactor.CSharp.Rewriting;
using RiktigRefactor.CSharp.Verification;

namespace RiktigRefactor.CSharp.MakeMethodStatic;

/// <summary>
/// Plans Make method static: finds the method, checks the preconditions, and writes the edits for its declaration, its body, and every call.
/// With <c>checkPreconditions</c> off it writes what it can and skips every check, for the naive implementation the tests compare against.
/// </summary>
public class MakeMethodStaticPlanner
{
    public const string ReceiverAnchor = "receiver";

    readonly Solution _solution;
    readonly IMethodSymbol _method;
    readonly MethodDeclarationSyntax _declaration;
    readonly DocumentId _declarationDocument;
    readonly string _receiverName;
    readonly bool _checkPreconditions;
    readonly CancellationToken _cancellationToken;
    readonly List<SourceEdit> _edits = [];

    MakeMethodStaticPlanner(Solution solution, IMethodSymbol method, MethodDeclarationSyntax declaration, string receiverName, bool checkPreconditions, CancellationToken cancellationToken)
    {
        this._solution = solution;
        this._method = method;
        this._declaration = declaration;
        this._declarationDocument = solution.GetDocumentId(declaration.SyntaxTree)!;
        this._receiverName = receiverName;
        this._checkPreconditions = checkPreconditions;
        this._cancellationToken = cancellationToken;
    }

    bool IsStruct => this._method.ContainingType.IsValueType;

    /// <summary>A struct receiver is passed by <c>in</c> when the method can't change it, and by <c>ref</c> otherwise.</summary>
    bool PassesReceiverByIn => this._method.IsReadOnly || this._method.ContainingType.IsReadOnly;

    public static async Task<(MakeMethodStaticPlan? Plan, RefusalReason? Refusal)> PlanAsync(
        Solution solution,
        DocumentId documentId,
        int position,
        string? receiverName,
        bool checkPreconditions,
        CancellationToken cancellationToken)
    {
        var document = solution.GetDocument(documentId) ?? throw new ArgumentException("Document not in solution.", nameof(documentId));
        var method = await MethodTargets.FindMethodAtAsync(document, position, cancellationToken).ConfigureAwait(false);
        if (method is null)
        {
            return (null, new RefusalReason(RefusalCodes.NoSymbol, "There is no method at the cursor.", null, "Put the cursor on the method's name."));
        }

        if (FindUnsupportedDeclaration(method, cancellationToken) is { } unsupported)
        {
            return (null, unsupported);
        }

        var declaration = (MethodDeclarationSyntax)method.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
        var name = receiverName ?? MethodTargets.LocalNameFor(method.ContainingType.Name);
        if (!MethodTargets.IsValidIdentifier(name))
        {
            return (null, new RefusalReason(RefusalCodes.InvalidIdentifier, $"`{name}` is not a valid C# identifier.", null, "Choose another receiver name."));
        }

        var planner = new MakeMethodStaticPlanner(solution, method, declaration, name, checkPreconditions, cancellationToken);
        var refusal = await planner.PlanAsync().ConfigureAwait(false);
        return refusal is null ? (new MakeMethodStaticPlan(method, planner._edits), null) : (null, refusal);
    }

    static RefusalReason? FindUnsupportedDeclaration(IMethodSymbol method, CancellationToken cancellationToken)
    {
        if (method.MethodKind != MethodKind.Ordinary || method.IsStatic)
        {
            return Refuse(RefusalCodes.NotInstanceMethod, $"`{method.Name}` is not an instance method.", method.Locations.FirstOrDefault(), null);
        }

        var isSupportedDeclaration = method.DeclaringSyntaxReferences is [var reference]
            && reference.GetSyntax(cancellationToken) is MethodDeclarationSyntax { Parent: ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax } declaration
            && (declaration.Body is not null || declaration.ExpressionBody is not null)
            && !method.IsExtern
            && !method.IsPartialDefinition
            && method.PartialImplementationPart is null
            && method.PartialDefinitionPart is null;
        return isSupportedDeclaration
            ? null
            : Refuse(RefusalCodes.NotInstanceMethod, $"`{method.Name}` is not an instance method with a body declared in a class, struct or record of the workspace.", method.Locations.FirstOrDefault(), null);
    }

    async Task<RefusalReason?> PlanAsync()
    {
        if (this._checkPreconditions && await FindDeclarationProblemAsync().ConfigureAwait(false) is { } declarationProblem)
        {
            return declarationProblem;
        }

        var callProblem = await PlanCallsAsync().ConfigureAwait(false);
        if (callProblem is not null)
        {
            return callProblem;
        }

        var bodyProblem = await PlanBodyAsync().ConfigureAwait(false);
        if (bodyProblem is not null)
        {
            return bodyProblem;
        }

        await PlanDeclarationAsync().ConfigureAwait(false);
        return null;
    }

    async Task<RefusalReason?> FindDeclarationProblemAsync()
    {
        var method = this._method;
        if (method.IsVirtual || method.IsAbstract || method.IsOverride || !method.ExplicitInterfaceImplementations.IsEmpty
            || await MethodTargets.ImplementsInterfaceMemberAsync(this._solution, method, this._cancellationToken).ConfigureAwait(false))
        {
            return Refuse(RefusalCodes.DynamicDispatch, $"`{method.Name}` takes part in dynamic dispatch (virtual, abstract, override, or an interface implementation), and a static method can't.", method.Locations[0], null);
        }

        if (MethodTargets.PatternNames.Contains(method.Name))
        {
            return Refuse(RefusalCodes.ImplicitReference, $"`{method.Name}` is a name the compiler or runtime can look up implicitly, so making it static could change code that does not name it.", method.Locations[0], null);
        }

        if (await MethodTargets.FindDisabledTextMentioningAsync(this._solution, method.Name, this._cancellationToken).ConfigureAwait(false) is { } disabled)
        {
            return Refuse(RefusalCodes.PreprocessorRegion, $"A conditionally compiled region mentions `{method.Name}`, so its calls can't be updated in every configuration.", disabled, null);
        }

        return null;
    }

    async Task<RefusalReason?> PlanCallsAsync()
    {
        var locations = await MethodTargets.FindReferenceLocationsAsync(this._solution, this._method, this._cancellationToken).ConfigureAwait(false);
        var references = new List<(SimpleNameSyntax Name, SemanticModel Model, DocumentId Document)>();
        foreach (var location in locations)
        {
            var document = this._solution.GetDocument(location.SourceTree)!;
            var root = await document.GetSyntaxRootAsync(this._cancellationToken).ConfigureAwait(false);
            var model = (await document.GetSemanticModelAsync(this._cancellationToken).ConfigureAwait(false))!;
            var token = root!.FindToken(location.SourceSpan.Start, findInsideTrivia: true);
            if (token.Parent is not SimpleNameSyntax name || name.Identifier.ValueText != this._method.Name)
            {
                if (this._checkPreconditions)
                {
                    return Refuse(RefusalCodes.ImplicitReference, $"`{this._method.Name}` is called implicitly here, and the call can't be rewritten.", location, null);
                }

                continue;
            }

            references.Add((name, model, document.Id));
        }

        // Outer calls first, so that a call's prefix goes before the prefix of a call in its receiver.
        foreach (var (name, model, documentId) in references.OrderBy(reference => reference.Document.Id).ThenBy(reference => CallOf(reference.Name)?.SpanStart ?? reference.Name.SpanStart).ThenByDescending(reference => CallOf(reference.Name)?.Span.Length ?? 0))
        {
            var problem = PlanReference(name, model, documentId);
            if (problem is not null)
            {
                return problem;
            }
        }

        return null;
    }

    static InvocationExpressionSyntax? CallOf(SimpleNameSyntax name) => name.Parent switch
    {
        InvocationExpressionSyntax invocation when invocation.Expression == name => invocation,
        MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax invocation } memberAccess when memberAccess.Name == name && invocation.Expression == memberAccess => invocation,
        _ => null,
    };

    RefusalReason? PlanReference(SimpleNameSyntax name, SemanticModel model, DocumentId documentId)
    {
        if (name.Ancestors().Any(ancestor => ancestor is CrefSyntax) || IsInsideNameof(model, name, this._cancellationToken))
        {
            return null;
        }

        if (this._checkPreconditions && ExpressionTrees.IsInsideExpressionTreeOrQuery(model, name, this._cancellationToken))
        {
            return Refuse(RefusalCodes.ExpressionTreeOrQuery, $"`{this._method.Name}` is called inside an expression tree or a query expression, whose shape would change.", name.GetLocation(), null);
        }

        if (name.Parent is MemberBindingExpressionSyntax)
        {
            return this._checkPreconditions
                ? Refuse(RefusalCodes.ReceiverMayBeNull, "The call is null-conditional, so its receiver may be null.", name.GetLocation(), null)
                : null;
        }

        if (CallOf(name) is not { } call)
        {
            return this._checkPreconditions
                ? Refuse(RefusalCodes.MethodGroupReference, $"`{this._method.Name}` is used as a method group, which binds the receiver; a lambda would change when the receiver is evaluated.", name.GetLocation(), "Write the delegate as a lambda that calls the method first.")
                : null;
        }

        var receiver = call.Expression is MemberAccessExpressionSyntax memberAccess ? memberAccess.Expression : null;
        if (receiver is BaseExpressionSyntax)
        {
            receiver = null;
        }

        if (this._checkPreconditions && FindReceiverProblem(model, call, receiver) is { } receiverProblem)
        {
            return receiverProblem;
        }

        var calledMethod = (IMethodSymbol?)model.GetSymbolInfo(name, this._cancellationToken).Symbol ?? this._method;
        var prefix = new List<SourcePiece>
        {
            new TypePiece(calledMethod.ContainingType.ToMinimalDisplayString(model, call.SpanStart), calledMethod.ContainingType),
            new LiteralPiece("."),
            new NamePiece(this._method.Name, new BindsToOriginal(calledMethod)),
        };
        if (name is GenericNameSyntax generic)
        {
            prefix.Add(new CopyPiece(documentId, generic.TypeArgumentList.Span));
        }

        prefix.Add(new LiteralPiece("("));
        if (IsStruct && !PassesReceiverByIn)
        {
            prefix.Add(new LiteralPiece("ref "));
        }

        var separator = call.ArgumentList.Arguments.Count == 0 ? string.Empty : ", ";
        if (receiver is not null)
        {
            this._edits.Add(SourceEdit.Insert(documentId, receiver.SpanStart, [.. prefix]));
            this._edits.Add(SourceEdit.Replace(documentId, TextSpan(receiver.Span.End, call.ArgumentList.OpenParenToken.Span.End), new LiteralPiece(separator)));
            return null;
        }

        var isInsideMethod = name.SyntaxTree == this._declaration.SyntaxTree && this._declaration.Span.Contains(name.Span);
        prefix.Add(isInsideMethod ? ReceiverName() : new LiteralPiece("this"));
        prefix.Add(new LiteralPiece(separator));
        this._edits.Add(SourceEdit.Replace(documentId, TextSpan(call.Expression.SpanStart, call.ArgumentList.OpenParenToken.Span.End), [.. prefix]));
        return null;
    }

    RefusalReason? FindReceiverProblem(SemanticModel model, InvocationExpressionSyntax call, ExpressionSyntax? receiver)
    {
        if (receiver is null)
        {
            return IsStruct && !PassesReceiverByIn && !IsInsideMethodOrMutableStructMember(model, call)
                ? Refuse(RefusalCodes.ReceiverNotVariable, "`this` is read-only here, so the call ran on a copy, and passing it by `ref` would not.", call.GetLocation(), null)
                : null;
        }

        var receiverType = model.GetTypeInfo(receiver, this._cancellationToken).Type;
        if (!IsSupportedReceiverType(receiverType))
        {
            return Refuse(RefusalCodes.UnsupportedReceiver, $"The receiver `{receiver}` has type `{receiverType}`, which can't be passed as the new parameter without a conversion that might change behavior.", receiver.GetLocation(), null);
        }

        if (!IsStruct && !NonNullExpressions.IsProvablyNonNull(model, receiver, this._cancellationToken))
        {
            return Refuse(RefusalCodes.ReceiverMayBeNull, $"The receiver `{receiver}` might be null. A null receiver fails before the body runs, but a static method would run the body.", receiver.GetLocation(), "Make sure the receiver is not null, for example by assigning it a new object in a local that is never assigned again.");
        }

        if (IsStruct && !PassesReceiverByIn && !IsMutableVariable(model.GetOperation(receiver, this._cancellationToken)))
        {
            return Refuse(RefusalCodes.ReceiverNotVariable, $"The receiver `{receiver}` is not a mutable variable, so the call ran on a copy, and passing it by `ref` would not.", receiver.GetLocation(), null);
        }

        return null;
    }

    bool IsSupportedReceiverType(ITypeSymbol? type)
    {
        if (type is not INamedTypeSymbol named)
        {
            return false;
        }

        if (IsStruct)
        {
            return SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, this._method.ContainingType.OriginalDefinition);
        }

        for (var current = named; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, this._method.ContainingType.OriginalDefinition))
            {
                return true;
            }
        }

        return false;
    }

    bool IsInsideMethodOrMutableStructMember(SemanticModel model, SyntaxNode call)
    {
        if (call.SyntaxTree == this._declaration.SyntaxTree && this._declaration.Span.Contains(call.Span))
        {
            return true;
        }

        var enclosing = model.GetEnclosingSymbol(call.SpanStart, this._cancellationToken);
        return enclosing is IMethodSymbol { IsReadOnly: false, IsStatic: false, MethodKind: not MethodKind.AnonymousFunction and not MethodKind.LocalFunction } member
            && !member.ContainingType.IsReadOnly;
    }

    static bool IsMutableVariable(IOperation? operation) => operation switch
    {
        ILocalReferenceOperation local => local.Local is { IsConst: false, IsForEach: false, IsUsing: false, IsFixed: false } && local.Local.RefKind is RefKind.None or RefKind.Ref,
        IParameterReferenceOperation parameter => parameter.Parameter.RefKind is RefKind.None or RefKind.Ref or RefKind.Out,
        IFieldReferenceOperation field => !field.Field.IsReadOnly && (field.Field.IsStatic || field.Instance?.Type?.IsReferenceType == true || IsMutableVariable(field.Instance)),
        IArrayElementReferenceOperation => true,
        _ => false,
    };

    static bool IsInsideNameof(SemanticModel model, SyntaxNode node, CancellationToken cancellationToken)
        => node.Ancestors().OfType<InvocationExpressionSyntax>().Any(invocation =>
            invocation.Expression is IdentifierNameSyntax { Identifier.ValueText: "nameof" }
            && model.GetOperation(invocation, cancellationToken) is INameOfOperation);

    async Task<RefusalReason?> PlanBodyAsync()
    {
        var document = this._solution.GetDocument(this._declarationDocument)!;
        var model = (await document.GetSemanticModelAsync(this._cancellationToken).ConfigureAwait(false))!;
        SyntaxNode body = (SyntaxNode?)this._declaration.Body ?? this._declaration.ExpressionBody!;
        foreach (var node in body.DescendantNodes())
        {
            switch (node)
            {
                case BaseExpressionSyntax when this._checkPreconditions:
                    return Refuse(RefusalCodes.BaseAccess, "The method uses `base`, which a static method can't.", node.GetLocation(), null);
                case ThisExpressionSyntax:
                    this._edits.Add(SourceEdit.Replace(this._declarationDocument, node.Span, ReceiverName()));
                    break;
                case SimpleNameSyntax name when IsImplicitInstanceAccess(model, name):
                    this._edits.Add(SourceEdit.Insert(this._declarationDocument, name.SpanStart, ReceiverName(), new LiteralPiece(".")));
                    break;
                default:
                    break;
            }
        }

        return null;
    }

    bool IsImplicitInstanceAccess(SemanticModel model, SimpleNameSyntax name)
    {
        if (name.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == name)
        {
            return false;
        }

        var operation = model.GetOperation(name, this._cancellationToken);
        if (operation is null && name.Parent is InvocationExpressionSyntax invocation && invocation.Expression == name)
        {
            operation = model.GetOperation(invocation, this._cancellationToken);
        }

        var instance = operation switch
        {
            IMemberReferenceOperation member when !SymbolEqualityComparer.Default.Equals(SymbolIdentity.Normalize(member.Member), this._method) => member.Instance,
            IInvocationOperation call when !SymbolEqualityComparer.Default.Equals(SymbolIdentity.Normalize(call.TargetMethod), this._method) => call.Instance,
            _ => null,
        };
        return instance is IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ContainingTypeInstance, IsImplicit: true };
    }

    async Task PlanDeclarationAsync()
    {
        var document = this._solution.GetDocument(this._declarationDocument)!;
        var model = (await document.GetSemanticModelAsync(this._cancellationToken).ConfigureAwait(false))!;
        var readonlyModifier = this._declaration.Modifiers.FirstOrDefault(modifier => modifier.IsKind(SyntaxKind.ReadOnlyKeyword));
        if (readonlyModifier != default)
        {
            this._edits.Add(SourceEdit.Delete(this._declarationDocument, readonlyModifier.FullSpan));
        }

        this._edits.Add(SourceEdit.Insert(this._declarationDocument, this._declaration.ReturnType.SpanStart, new LiteralPiece("static ")));

        var parameters = new List<SourcePiece>();
        if (IsStruct)
        {
            parameters.Add(new LiteralPiece(PassesReceiverByIn ? "in " : "ref "));
        }

        var type = this._method.ContainingType;
        parameters.Add(new TypePiece(type.ToMinimalDisplayString(model, this._declaration.ParameterList.SpanStart), type));
        parameters.Add(new LiteralPiece(" "));
        parameters.Add(new DeclarationPiece(this._receiverName, ReceiverAnchor));
        if (this._declaration.ParameterList.Parameters.Count > 0)
        {
            parameters.Add(new LiteralPiece(", "));
        }

        this._edits.Add(SourceEdit.Insert(this._declarationDocument, this._declaration.ParameterList.OpenParenToken.Span.End, [.. parameters]));
    }

    NamePiece ReceiverName() => new(this._receiverName, new BindsToDeclaration(ReceiverAnchor));

    static Microsoft.CodeAnalysis.Text.TextSpan TextSpan(int start, int end) => Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(start, end);

    static RefusalReason Refuse(string code, string message, Location? location, string? suggestion)
        => new(code, message, location is null ? null : Ranges.FromLocation(location), suggestion);
}
