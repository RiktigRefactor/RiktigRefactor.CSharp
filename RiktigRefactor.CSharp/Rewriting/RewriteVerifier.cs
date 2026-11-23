using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RiktigRefactor.CSharp.Protocol;
using RiktigRefactor.CSharp.Verification;

namespace RiktigRefactor.CSharp.Rewriting;

/// <summary>
/// Re-binds a <see cref="RewrittenSolution"/> and checks that:
/// every original name that survived binds to the same declaration as before;
/// every original name that disappeared was meant to;
/// every name an edit wrote binds to what it expects;
/// every type an edit wrote denotes the expected type;
/// and no name appears that nothing accounts for.
/// </summary>
public class RewriteVerifier
{
    static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    readonly RewrittenSolution _rewrite;
    readonly Func<SimpleNameSyntax, SemanticModel, bool> _mayDisappear;

    RewriteVerifier(RewrittenSolution rewrite, Func<SimpleNameSyntax, SemanticModel, bool> mayDisappear)
    {
        this._rewrite = rewrite;
        this._mayDisappear = mayDisappear;
    }

    /// <param name="mayDisappear">True for an original name (with the original document's model) that the refactoring deletes on purpose.</param>
    public static async Task<RefusalReason?> FindProblemAsync(
        RewrittenSolution rewrite,
        Func<SimpleNameSyntax, SemanticModel, bool> mayDisappear,
        CancellationToken cancellationToken)
    {
        var verifier = new RewriteVerifier(rewrite, mayDisappear);
        return await verifier.FindOriginalNameProblemAsync(cancellationToken).ConfigureAwait(false)
            ?? await verifier.FindWrittenNameProblemAsync(cancellationToken).ConfigureAwait(false)
            ?? await verifier.FindWrittenTypeProblemAsync(cancellationToken).ConfigureAwait(false)
            ?? await verifier.FindUnaccountedNameAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>The display of a type that two compilations agree on exactly when they denote the same type.</summary>
    public static string DescribeType(ITypeSymbol type) => $"{type.ToDisplayString(TypeFormat)}@{type.OriginalDefinition.ContainingAssembly?.Name}";

    /// <summary>Checks every original name in the solution, since an unchanged file can see a new overload or a moved member.</summary>
    async Task<RefusalReason?> FindOriginalNameProblemAsync(CancellationToken cancellationToken)
    {
        foreach (var oldDocument in this._rewrite.OldSolution.Projects.SelectMany(project => project.Documents))
        {
            var documentId = oldDocument.Id;
            var oldModel = (await oldDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false))!;
            var oldRoot = await oldDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            foreach (var oldName in oldRoot!.DescendantNodes(descendIntoTrivia: true).OfType<SimpleNameSyntax>())
            {
                var problem = await CheckOriginalNameAsync(documentId, oldModel, oldName, cancellationToken).ConfigureAwait(false);
                if (problem is not null)
                {
                    return problem;
                }
            }
        }

        return null;
    }

    async Task<RefusalReason?> CheckOriginalNameAsync(DocumentId documentId, SemanticModel oldModel, SimpleNameSyntax oldName, CancellationToken cancellationToken)
    {
        if (this._rewrite.MapOld(documentId, oldName.Identifier.SpanStart) is not { } mapped)
        {
            return this._mayDisappear(oldName, oldModel)
                ? null
                : Internal(oldName, $"`{oldName.Identifier.ValueText}` was rewritten unexpectedly.");
        }

        var newName = await FindNameAsync(this._rewrite.NewSolution, mapped.Document, mapped.Start, oldName.Identifier.ValueText, cancellationToken).ConfigureAwait(false);
        if (newName is null)
        {
            return Internal(oldName, $"`{oldName.Identifier.ValueText}` could not be found after the change.");
        }

        var newModel = (await this._rewrite.NewSolution.GetDocument(mapped.Document)!.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false))!;
        var expected = BoundSymbols(oldModel, oldName, cancellationToken).Select(ExpectedNewId).Order(StringComparer.Ordinal);
        var actual = BoundSymbols(newModel, newName, cancellationToken).Select(NewId).Order(StringComparer.Ordinal);
        return expected.SequenceEqual(actual, StringComparer.Ordinal)
            ? null
            : BindingChanged(oldName, $"`{oldName.Identifier.ValueText}` would refer to a different declaration.");
    }

    async Task<RefusalReason?> FindWrittenNameProblemAsync(CancellationToken cancellationToken)
    {
        foreach (var written in this._rewrite.Names)
        {
            var newName = await FindNameAsync(this._rewrite.NewSolution, written.Document, written.Start, Unescaped(written.Piece.Text), cancellationToken).ConfigureAwait(false);
            if (newName is null)
            {
                return Internal(null, $"The written name `{written.Piece.Text}` could not be found.");
            }

            var newModel = (await this._rewrite.NewSolution.GetDocument(written.Document)!.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false))!;
            var actual = BoundSymbols(newModel, newName, cancellationToken);
            if (actual.Count != 1 || !IsExpected(actual[0], written.Piece.Expected))
            {
                return BindingChanged(newName, $"The new `{written.Piece.Text}` would not refer to the intended declaration.");
            }
        }

        return null;
    }

    async Task<RefusalReason?> FindWrittenTypeProblemAsync(CancellationToken cancellationToken)
    {
        foreach (var written in this._rewrite.Types)
        {
            var document = this._rewrite.NewSolution.GetDocument(written.Document)!;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var model = (await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false))!;
            var node = root!.FindNode(written.Span, findInsideTrivia: true, getInnermostNodeForTie: false);
            var actual = node is TypeSyntax or ExpressionSyntax ? model.GetTypeInfo(node, cancellationToken).Type ?? model.GetSymbolInfo(node, cancellationToken).Symbol as ITypeSymbol : null;
            if (node.Span != written.Span || actual is null || DescribeType(actual) != DescribeType(written.Expected))
            {
                return BindingChanged(node as SimpleNameSyntax, $"The type `{node}` would not denote `{written.Expected}`.");
            }
        }

        return null;
    }

    async Task<RefusalReason?> FindUnaccountedNameAsync(CancellationToken cancellationToken)
    {
        foreach (var documentId in this._rewrite.ChangedDocuments)
        {
            var root = await this._rewrite.NewSolution.GetDocument(documentId)!.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            foreach (var name in root!.DescendantNodes(descendIntoTrivia: true).OfType<SimpleNameSyntax>())
            {
                var start = name.Identifier.SpanStart;
                var isAccounted = this._rewrite.IsOriginal(documentId, start)
                    || this._rewrite.Names.Any(written => written.Document == documentId && written.Start == start)
                    || this._rewrite.Types.Any(written => written.Document == documentId && written.Span.Contains(start));
                if (!isAccounted)
                {
                    return Internal(name, $"The edit wrote `{name}`, which nothing checks.");
                }
            }
        }

        return null;
    }

    static async Task<SimpleNameSyntax?> FindNameAsync(Solution solution, DocumentId documentId, int start, string valueText, CancellationToken cancellationToken)
    {
        var root = await solution.GetDocument(documentId)!.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var token = root!.FindToken(start, findInsideTrivia: true);
        return token.SpanStart == start && token.ValueText == valueText && token.Parent is SimpleNameSyntax name ? name : null;
    }

    static string Unescaped(string text) => text.StartsWith('@') ? text[1..] : text;

    string ExpectedNewId(ISymbol oldSymbol)
    {
        var definition = SymbolIdentity.Normalize(oldSymbol);
        if (definition is INamespaceSymbol)
        {
            return NamespaceId(definition);
        }

        var oldId = SymbolIdentity.Describe(this._rewrite.OldSolution, oldSymbol) + TypeArguments(oldSymbol);

        var location = SymbolIdentity.PreferredSourceLocation(this._rewrite.OldSolution, definition);
        var documentId = SymbolIdentity.RegularDocumentId(this._rewrite.OldSolution, location?.SourceTree);
        if (location is null || documentId is null)
        {
            return oldId;
        }

        return this._rewrite.MapOld(documentId, location.SourceSpan.Start) is { } mapped
            ? SymbolIdentity.SourceId(definition, mapped.Document, mapped.Start) + TypeArguments(oldSymbol)
            : $"deleted:{oldId}";
    }

    string NewId(ISymbol newSymbol)
        => newSymbol is INamespaceSymbol
            ? NamespaceId(newSymbol)
            : SymbolIdentity.Describe(this._rewrite.NewSolution, newSymbol) + TypeArguments(newSymbol);

    bool IsExpected(ISymbol newSymbol, NameExpectation expectation) => expectation switch
    {
        BindsToOriginal original => NewId(newSymbol) == ExpectedNewId(original.Symbol),
        BindsToDeclaration declaration => IsDeclaredAt(newSymbol, declaration.Anchor),
        _ => false,
    };

    bool IsDeclaredAt(ISymbol newSymbol, string anchor)
        => this._rewrite.FindDeclaration(anchor) is { } declaration
            && newSymbol.Locations.Any(location => location.IsInSource
                && this._rewrite.NewSolution.GetDocumentId(location.SourceTree) == declaration.Document
                && location.SourceSpan.Start == declaration.Start);

    static string NamespaceId(ISymbol symbol) => "namespace:" + symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    /// <summary>A method call must also keep its type arguments, whether written or inferred.</summary>
    static string TypeArguments(ISymbol symbol)
        => symbol is IMethodSymbol { IsGenericMethod: true } method ? "<" + string.Join(",", method.TypeArguments.Select(DescribeType)) + ">" : string.Empty;

    static List<ISymbol> BoundSymbols(SemanticModel model, SimpleNameSyntax name, CancellationToken cancellationToken)
    {
        if (name is IdentifierNameSyntax identifier && model.GetAliasInfo(identifier, cancellationToken) is { } alias)
        {
            return [alias];
        }

        var info = model.GetSymbolInfo(name, cancellationToken);
        return info.Symbol is { } symbol ? [symbol] : [.. info.CandidateSymbols];
    }

    static RefusalReason BindingChanged(SyntaxNode? node, string message)
        => new(RefusalCodes.BindingWouldChange, message, node is null ? null : Ranges.FromSpan(node.SyntaxTree, node.Span), null);

    static RefusalReason Internal(SyntaxNode? node, string message)
        => new(RefusalCodes.InternalVerificationFailed, message, node is null ? null : Ranges.FromSpan(node.SyntaxTree, node.Span), "Please report this as a bug.");
}
