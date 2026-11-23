using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RiktigRefactor.CSharp.Protocol;

namespace RiktigRefactor.CSharp.Verification;

/// <summary>
/// Checks that every name in the original solution binds to the same symbol after a change,
/// allowing only the intended renames.
/// </summary>
public class BindingVerifier
{
    readonly Solution _oldSolution;
    readonly Solution _newSolution;
    readonly IReadOnlyDictionary<string, string> _renamedSymbolIdsToNewNames;
    readonly Dictionary<DocumentId, DocumentChangeMap> _changeMaps = [];

    BindingVerifier(Solution oldSolution, Solution newSolution, IReadOnlyDictionary<string, string> renamedSymbolIdsToNewNames)
    {
        this._oldSolution = oldSolution;
        this._newSolution = newSolution;
        this._renamedSymbolIdsToNewNames = renamedSymbolIdsToNewNames;
    }

    /// <param name="renamedSymbols">Symbols in <paramref name="oldSolution"/> that were intentionally renamed, with their new names.</param>
    public static async Task<RefusalReason?> FindFirstBindingChangeAsync(
        Solution oldSolution,
        Solution newSolution,
        IReadOnlyDictionary<ISymbol, string> renamedSymbols,
        CancellationToken cancellationToken)
    {
        var renamedIds = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (symbol, newName) in renamedSymbols)
        {
            renamedIds[OldSymbolId(oldSolution, symbol)] = newName;
        }

        var verifier = new BindingVerifier(oldSolution, newSolution, renamedIds);
        return await verifier.FindFirstBindingChangeAsync(cancellationToken).ConfigureAwait(false);
    }

    static string OldSymbolId(Solution solution, ISymbol symbol) => SymbolIdentity.Describe(solution, symbol);

    async Task<RefusalReason?> FindFirstBindingChangeAsync(CancellationToken cancellationToken)
    {
        foreach (var oldDocument in this._oldSolution.Projects.SelectMany(project => project.Documents))
        {
            var newDocument = this._newSolution.GetDocument(oldDocument.Id);
            if (newDocument is null)
            {
                return InternalFailure($"Document {oldDocument.FilePath ?? oldDocument.Name} disappeared.");
            }

            var oldModel = await oldDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var newModel = await newDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var oldRoot = await oldDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (oldModel is null || newModel is null || oldRoot is null)
            {
                continue;
            }

            var map = await GetChangeMapAsync(oldDocument.Id, cancellationToken).ConfigureAwait(false);
            if (map.UnexpectedChange is { } unexpected)
            {
                return new RefusalReason(
                    RefusalCodes.BindingWouldChange,
                    $"Renaming would require rewriting `{Truncate(unexpected.ToString().Trim())}`, beyond changing and qualifying names, which can't be verified.",
                    Ranges.FromSpan(unexpected.SyntaxTree!, unexpected.Span),
                    null);
            }

            foreach (var oldName in oldRoot.DescendantNodes(descendIntoTrivia: true).OfType<SimpleNameSyntax>())
            {
                var problem = await CheckNameAsync(oldDocument, oldModel, newModel, oldName, cancellationToken).ConfigureAwait(false);
                if (problem is not null)
                {
                    return problem;
                }
            }
        }

        return null;
    }

    async Task<RefusalReason?> CheckNameAsync(
        Document oldDocument,
        SemanticModel oldModel,
        SemanticModel newModel,
        SimpleNameSyntax oldName,
        CancellationToken cancellationToken)
    {
        var oldSymbols = BoundSymbols(oldModel, oldName, cancellationToken);
        var oldIds = oldSymbols.Select(symbol => SymbolIdentity.Describe(this._oldSolution, symbol)).ToList();
        var renamedTo = oldIds.Select(id => this._renamedSymbolIdsToNewNames.GetValueOrDefault(id)).FirstOrDefault(name => name is not null)
            ?? oldSymbols.Select(RenamedTypeNameIfConstructor).FirstOrDefault(name => name is not null);
        var expectedText = oldName.IsVar ? oldName.Identifier.ValueText : renamedTo ?? oldName.Identifier.ValueText;

        var map = await GetChangeMapAsync(oldDocument.Id, cancellationToken).ConfigureAwait(false);
        var newToken = map.FindNewToken(oldName.Identifier.SpanStart, expectedText)
            ?? (AttributeShortName(expectedText) is { } shortName ? map.FindNewToken(oldName.Identifier.SpanStart, shortName) : null);
        if (newToken?.Parent is not SimpleNameSyntax newName)
        {
            return BindingChanged(oldName, $"`{oldName.Identifier.ValueText}` could not be found after the change.");
        }

        var expectedNewIds = new List<string>();
        foreach (var oldSymbol in oldSymbols)
        {
            expectedNewIds.Add(await ExpectedNewIdAsync(oldSymbol, cancellationToken).ConfigureAwait(false));
        }

        var newIds = BoundSymbols(newModel, newName, cancellationToken)
            .Select(symbol => SymbolIdentity.Describe(this._newSolution, symbol))
            .ToList();

        if (!expectedNewIds.Order(StringComparer.Ordinal).SequenceEqual(newIds.Order(StringComparer.Ordinal), StringComparer.Ordinal))
        {
            return BindingChanged(oldName, $"`{oldName.Identifier.ValueText}` would refer to a different declaration.");
        }

        return null;
    }

    async Task<string> ExpectedNewIdAsync(ISymbol oldSymbol, CancellationToken cancellationToken)
    {
        var definition = SymbolIdentity.Normalize(oldSymbol);
        if (definition is INamespaceSymbol { IsGlobalNamespace: true })
        {
            return SymbolIdentity.Describe(this._oldSolution, oldSymbol);
        }

        var sourceLocation = SymbolIdentity.PreferredSourceLocation(this._oldSolution, definition);
        var oldDocumentId = SymbolIdentity.RegularDocumentId(this._oldSolution, sourceLocation?.SourceTree);
        if (sourceLocation is null || oldDocumentId is null)
        {
            return SymbolIdentity.Describe(this._oldSolution, oldSymbol);
        }

        var oldId = SymbolIdentity.Describe(this._oldSolution, oldSymbol);
        var expectedName = this._renamedSymbolIdsToNewNames.GetValueOrDefault(oldId)
            ?? RenamedTypeNameIfConstructor(definition)
            ?? SymbolIdentity.DeclaringTokenText(definition);
        var map = await GetChangeMapAsync(oldDocumentId, cancellationToken).ConfigureAwait(false);
        var newToken = map.FindNewToken(sourceLocation.SourceSpan.Start, expectedName);
        return newToken is { } token
            ? SymbolIdentity.SourceId(definition, oldDocumentId, token.SpanStart)
            : $"missing-declaration:{oldId}";
    }

    /// <summary>
    /// An attribute may be written without its <c>Attribute</c> suffix, so a renamed attribute type may be spelled either way.
    /// Either spelling is accepted here, since the binding check that follows decides whether it means the same.
    /// </summary>
    static string? AttributeShortName(string name)
        => name.Length > "Attribute".Length && name.EndsWith("Attribute", StringComparison.Ordinal) ? name[..^"Attribute".Length] : null;

    /// <summary>A constructor is declared by its type's name, so renaming the type renames that token.</summary>
    string? RenamedTypeNameIfConstructor(ISymbol definition)
        => definition is IMethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor or MethodKind.Destructor } constructor
            ? this._renamedSymbolIdsToNewNames.GetValueOrDefault(OldSymbolId(this._oldSolution, constructor.ContainingType))
            : null;

    static List<ISymbol> BoundSymbols(SemanticModel model, SimpleNameSyntax name, CancellationToken cancellationToken)
    {
        if (name is IdentifierNameSyntax identifier && model.GetAliasInfo(identifier, cancellationToken) is { } usedAlias)
        {
            return [usedAlias];
        }

        var info = model.GetSymbolInfo(name, cancellationToken);
        if (info.Symbol is { } symbol)
        {
            return [symbol];
        }

        if (!info.CandidateSymbols.IsEmpty)
        {
            return [.. info.CandidateSymbols];
        }

        if (name.Parent is NameEqualsSyntax { Parent: UsingDirectiveSyntax usingDirective }
            && model.GetDeclaredSymbol(usingDirective, cancellationToken) is { } alias)
        {
            return [alias];
        }

        return [];
    }

    async Task<DocumentChangeMap> GetChangeMapAsync(DocumentId documentId, CancellationToken cancellationToken)
    {
        if (this._changeMaps.TryGetValue(documentId, out var existing))
        {
            return existing;
        }

        var oldRoot = await this._oldSolution.GetDocument(documentId)!.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var newRoot = await this._newSolution.GetDocument(documentId)!.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var map = new DocumentChangeMap(oldRoot!, newRoot!);
        this._changeMaps[documentId] = map;
        return map;
    }

    static string Truncate(string text) => text.Length <= 60 ? text : text[..60] + "...";

    static RefusalReason BindingChanged(SimpleNameSyntax oldName, string message)
        => new(RefusalCodes.BindingWouldChange, message, Ranges.FromSpan(oldName.SyntaxTree, oldName.Identifier.Span), null);

    static RefusalReason InternalFailure(string message)
        => new(RefusalCodes.InternalVerificationFailed, message, null, "Please report this as a bug.");
}
