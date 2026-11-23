using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using RiktigRefactor.CSharp.Verification;

namespace RiktigRefactor.CSharp.Rename;

/// <summary>
/// Roslyn's renamer qualifies a renamed static member when a local captures a reference to it, but not when a lambda parameter does,
/// because a lambda parameter may shadow the name.
/// After the rename, this qualifies such references with the member's type (<c>Program.value</c>).
/// The binding verifier then checks the result like any other rename.
/// </summary>
public static class CapturedStaticMemberReferences
{
    public static async Task<Solution> QualifyAsync(
        Solution oldSolution,
        Solution newSolution,
        ISymbol target,
        string newName,
        CancellationToken cancellationToken)
    {
        if (!target.IsStatic || target is not (IFieldSymbol or IPropertySymbol or IMethodSymbol or IEventSymbol) || target.ContainingType is null)
        {
            return newSolution;
        }

        var references = await SymbolFinder.FindReferencesAsync(target, oldSolution, cancellationToken).ConfigureAwait(false);
        var locationsByDocument = references
            .Where(reference => SymbolEqualityComparer.Default.Equals(reference.Definition, target))
            .SelectMany(reference => reference.Locations)
            .GroupBy(location => location.Document.Id);
        var qualified = newSolution;
        foreach (var locations in locationsByDocument)
        {
            var oldRoot = await oldSolution.GetDocument(locations.Key)!.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newDocument = newSolution.GetDocument(locations.Key)!;
            var newRoot = await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newModel = await newDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var map = new DocumentChangeMap(oldRoot!, newRoot!);
            var captured = locations
                .Select(location => map.FindNewToken(location.Location.SourceSpan.Start, newName)?.Parent)
                .OfType<IdentifierNameSyntax>()
                .Where(name => !IsQualified(name) && IsLocalLike(newModel!.GetSymbolInfo(name, cancellationToken).Symbol))
                .ToList();
            if (captured.Count == 0)
            {
                continue;
            }

            var withQualifiedNames = newRoot!.ReplaceNodes(captured, (original, _) => Qualify(original, target.ContainingType, newModel!));
            qualified = qualified.WithDocumentSyntaxRoot(locations.Key, withQualifiedNames);
        }

        return qualified;
    }

    static bool IsLocalLike(ISymbol? symbol) => symbol is ILocalSymbol or IParameterSymbol or IRangeVariableSymbol;

    static bool IsQualified(IdentifierNameSyntax name) => name.Parent switch
    {
        MemberAccessExpressionSyntax access => access.Name == name,
        QualifiedNameSyntax qualifiedName => qualifiedName.Right == name,
        MemberBindingExpressionSyntax or NameColonSyntax or NameEqualsSyntax => true,
        _ => false,
    };

    static MemberAccessExpressionSyntax Qualify(IdentifierNameSyntax name, INamedTypeSymbol containingType, SemanticModel model)
    {
        var typeName = SyntaxFactory.ParseExpression(TypeNameAt(containingType, model, name.SpanStart));
        return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, typeName, name.WithoutTrivia())
            .WithTriviaFrom(name);
    }

    /// <summary>
    /// The type's short name if it binds to the type at <paramref name="position"/>, or else its fully qualified name.
    /// <paramref name="type"/> comes from the solution before the rename, so it is compared by name with what binds in <paramref name="model"/>.
    /// </summary>
    static string TypeNameAt(INamedTypeSymbol type, SemanticModel model, int position)
    {
        var shortName = type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var fullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var bound = model.GetSpeculativeSymbolInfo(position, SyntaxFactory.ParseExpression(shortName), SpeculativeBindingOption.BindAsExpression).Symbol;
        return bound is INamedTypeSymbol boundType && boundType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == fullName ? shortName : fullName;
    }
}
