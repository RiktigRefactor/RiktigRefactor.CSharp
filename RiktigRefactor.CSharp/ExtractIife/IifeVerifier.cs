using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RiktigRefactor.CSharp.Verification;

namespace RiktigRefactor.CSharp.ExtractIife;

/// <summary>
/// Re-binds the document after extraction and checks that every name binds as before:
/// variables from outside bind to the lambda's parameters or locals, and everything else to the same declaration.
/// For an expression, also checks that its type, conversion, and every enclosing expression's type and symbol are unchanged.
/// </summary>
public class IifeVerifier
{
    static readonly SymbolDisplayFormat SignatureFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .WithMemberOptions(SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeRef)
        .WithParameterOptions(SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeParamsRefOut);

    readonly Solution _oldSolution;
    readonly Solution _newSolution;
    readonly SemanticModel _oldModel;
    readonly SemanticModel _newModel;
    readonly IifePlan _plan;
    readonly IifeText _text;
    readonly HashSet<ISymbol> _variablesFromOutside;
    readonly HashSet<ISymbol> _variablesDeclaredInSelectionAndUsedAfter;

    IifeVerifier(Solution oldSolution, Solution newSolution, SemanticModel oldModel, SemanticModel newModel, IifePlan plan, IifeText text)
    {
        this._oldSolution = oldSolution;
        this._newSolution = newSolution;
        this._oldModel = oldModel;
        this._newModel = newModel;
        this._plan = plan;
        this._text = text;
        this._variablesFromOutside = plan.VariablesFromOutside.ToHashSet(SymbolEqualityComparer.Default);
        this._variablesDeclaredInSelectionAndUsedAfter = plan.VariablesDeclaredInSelectionAndReturned.ToHashSet(SymbolEqualityComparer.Default);
    }

    public static async Task<string?> FindProblemAsync(
        Document oldDocument,
        Document newDocument,
        IifePlan plan,
        IifeText text,
        CancellationToken cancellationToken)
    {
        var oldModel = await oldDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var newModel = await newDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var verifier = new IifeVerifier(oldDocument.Project.Solution, newDocument.Project.Solution, oldModel!, newModel!, plan, text);
        return verifier.FindNameProblem(cancellationToken) ?? verifier.FindExpressionProblem(cancellationToken);
    }

    string? FindNameProblem(CancellationToken cancellationToken)
    {
        var oldRoot = this._oldModel.SyntaxTree.GetRoot(cancellationToken);
        var newRoot = this._newModel.SyntaxTree.GetRoot(cancellationToken);
        foreach (var oldName in oldRoot.DescendantNodes(descendIntoTrivia: true).OfType<SimpleNameSyntax>())
        {
            var newToken = newRoot.FindToken(this._text.MapOffset(oldName.Identifier.SpanStart), findInsideTrivia: true);
            if (newToken.Parent is not SimpleNameSyntax newName || newName.Identifier.ValueText != oldName.Identifier.ValueText)
            {
                return $"`{oldName}` at {oldName.SpanStart} could not be found after the change.";
            }

            var oldSymbols = BoundSymbols(this._oldModel, oldName, cancellationToken);
            var newSymbols = BoundSymbols(this._newModel, newName, cancellationToken);
            var isInsideSelection = this._plan.Selection.Span.Contains(oldName.Span);
            var matches = oldSymbols.Count == newSymbols.Count && oldSymbols.Zip(newSymbols).All(pair => Matches(pair.First, pair.Second, isInsideSelection));
            if (!matches)
            {
                return $"`{oldName}` at {oldName.SpanStart} would bind to a different declaration.";
            }
        }

        return null;
    }

    bool Matches(ISymbol oldSymbol, ISymbol newSymbol, bool isInsideSelection)
    {
        if (isInsideSelection && this._variablesFromOutside.Contains(oldSymbol))
        {
            return IsVariableDeclaredIn(newSymbol, oldSymbol.Name, this._text.DeclarationsSpan);
        }

        if (!isInsideSelection && this._variablesDeclaredInSelectionAndUsedAfter.Contains(oldSymbol))
        {
            return IsVariableDeclaredIn(newSymbol, oldSymbol.Name, this._text.CallSiteDeclarationsSpan);
        }

        if (oldSymbol.ToDisplayString(SignatureFormat) != newSymbol.ToDisplayString(SignatureFormat) || oldSymbol.Kind != newSymbol.Kind)
        {
            return false;
        }

        var oldLocation = oldSymbol.Locations.FirstOrDefault(location => location.SourceTree == this._oldModel.SyntaxTree);
        if (oldLocation is not null)
        {
            var expected = this._text.MapOffset(oldLocation.SourceSpan.Start);
            return newSymbol.Locations.Any(location => location.SourceTree == this._newModel.SyntaxTree && location.SourceSpan.Start == expected);
        }

        return SymbolIdentity.Describe(this._oldSolution, oldSymbol) == SymbolIdentity.Describe(this._newSolution, newSymbol);
    }

    bool IsVariableDeclaredIn(ISymbol symbol, string name, Microsoft.CodeAnalysis.Text.TextSpan span)
        => symbol is IParameterSymbol or ILocalSymbol
            && symbol.Name == name
            && symbol.Locations.Any(location => location.SourceTree == this._newModel.SyntaxTree && span.Contains(location.SourceSpan));

    string? FindExpressionProblem(CancellationToken cancellationToken)
    {
        if (this._plan.Selection.Expression is not { } oldExpression)
        {
            return null;
        }

        var newRoot = this._newModel.SyntaxTree.GetRoot(cancellationToken);
        var newSpan = new Microsoft.CodeAnalysis.Text.TextSpan(this._text.ReplacedSpan.Start, this._text.NewText.Length);
        var newExpression = newRoot.FindNode(newSpan, getInnermostNodeForTie: false)
            .AncestorsAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(node => node.Span == newSpan);
        if (newExpression is null)
        {
            return "The lambda call could not be found after the change.";
        }

        SyntaxNode oldNode = oldExpression;
        SyntaxNode newNode = newExpression;
        var isSelectedNode = true;
        while (true)
        {
            var includeSymbol = !isSelectedNode && !MirrorsItsOperand(oldNode);
            if (Describe(this._oldModel, oldNode, includeSymbol, cancellationToken) != Describe(this._newModel, newNode, includeSymbol, cancellationToken))
            {
                return $"`{oldNode}` would have a different type, conversion, or symbol.";
            }

            if (oldNode.Parent is null || newNode.Parent is null || oldNode is StatementSyntax or MemberDeclarationSyntax)
            {
                return null;
            }

            oldNode = oldNode.Parent;
            newNode = newNode.Parent;
            isSelectedNode = false;
            if (oldNode.RawKind != newNode.RawKind)
            {
                return $"`{oldNode}` would have a different shape.";
            }
        }
    }

    /// <summary>Roslyn reports the symbol of the expression inside parentheses or <c>checked(...)</c> as theirs, and that expression is the selection, now a delegate invocation.</summary>
    static bool MirrorsItsOperand(SyntaxNode node) => node is ParenthesizedExpressionSyntax or CheckedExpressionSyntax;

    /// <param name="includeSymbol">False for the selected expression itself, which becomes a delegate invocation.</param>
    static string Describe(SemanticModel model, SyntaxNode node, bool includeSymbol, CancellationToken cancellationToken)
    {
        var typeInfo = model.GetTypeInfo(node, cancellationToken);
        var symbol = model.GetSymbolInfo(node, cancellationToken).Symbol;
        var conversion = node is ExpressionSyntax expression ? model.GetConversion(expression, cancellationToken) : default;
        var conversionText = $"{conversion.Exists}/{conversion.IsIdentity}/{conversion.IsImplicit}/{conversion.IsNumeric}/{conversion.IsReference}/{conversion.IsBoxing}/{conversion.IsNullable}/{conversion.IsUserDefined}/{conversion.MethodSymbol?.ToDisplayString(SignatureFormat)}";
        var symbolText = includeSymbol ? symbol?.ToDisplayString(SignatureFormat) : null;
        return $"{typeInfo.Type?.ToDisplayString(SignatureFormat)}|{typeInfo.ConvertedType?.ToDisplayString(SignatureFormat)}|{conversionText}|{symbolText}";
    }

    static List<ISymbol> BoundSymbols(SemanticModel model, SimpleNameSyntax name, CancellationToken cancellationToken)
    {
        if (name is IdentifierNameSyntax identifier && model.GetAliasInfo(identifier, cancellationToken) is { } alias)
        {
            return [alias];
        }

        var info = model.GetSymbolInfo(name, cancellationToken);
        if (info.Symbol is { } symbol)
        {
            return [symbol];
        }

        return [.. info.CandidateSymbols];
    }
}
