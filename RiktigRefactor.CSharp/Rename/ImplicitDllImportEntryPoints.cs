using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RiktigRefactor.CSharp.Rename;

/// <summary>
/// A <c>[DllImport]</c> method with no <c>EntryPoint</c> calls the native function with the method's own name,
/// so renaming the method would make it call a different function.
/// Writing the entry point explicitly (<c>EntryPoint = "strlen"</c>) keeps it when the method is renamed.
/// </summary>
public static class ImplicitDllImportEntryPoints
{
    /// <summary>
    /// Writes the entry point explicitly if <paramref name="symbol"/> is a <c>[DllImport]</c> method that leaves it implicit.
    /// Returns null if the method didn't keep exactly the same entry point, which would mean the rewrite is wrong.
    /// </summary>
    public static async Task<(Solution Solution, ISymbol Symbol)?> MakeExplicitAsync(Solution solution, ISymbol symbol, CancellationToken cancellationToken)
    {
        if (symbol is not IMethodSymbol method || method.GetDllImportData() is null)
        {
            return (solution, symbol);
        }

        var explicitSolution = solution;
        foreach (var reference in method.DeclaringSyntaxReferences)
        {
            var document = explicitSolution.GetDocument(reference.SyntaxTree);
            var declaration = await reference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
            var model = await document!.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var dllImport = FindDllImportAttribute((MethodDeclarationSyntax)declaration, model!, cancellationToken);
            if (dllImport is null || HasExplicitEntryPoint(dllImport))
            {
                continue;
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            explicitSolution = explicitSolution.WithDocumentSyntaxRoot(document.Id, root!.ReplaceNode(dllImport, WithEntryPoint(dllImport, method.Name)));
        }

        var explicitSymbol = await SymbolsAcrossChanges.FindSameSymbolAsync(solution, explicitSolution, symbol, cancellationToken).ConfigureAwait(false);
        if (explicitSymbol is not IMethodSymbol explicitMethod || explicitMethod.GetDllImportData()?.EntryPointName != method.GetDllImportData()!.EntryPointName)
        {
            return null;
        }

        return (explicitSolution, explicitSymbol);
    }

    static AttributeSyntax? FindDllImportAttribute(MethodDeclarationSyntax declaration, SemanticModel model, CancellationToken cancellationToken)
    {
        var dllImportType = model.Compilation.GetTypeByMetadataName("System.Runtime.InteropServices.DllImportAttribute");
        return declaration.AttributeLists
            .SelectMany(list => list.Attributes)
            .FirstOrDefault(attribute => SymbolEqualityComparer.Default.Equals(
                model.GetSymbolInfo(attribute, cancellationToken).Symbol?.ContainingType,
                dllImportType));
    }

    static bool HasExplicitEntryPoint(AttributeSyntax dllImport)
        => dllImport.ArgumentList?.Arguments.Any(argument => argument.NameEquals?.Name.Identifier.ValueText == "EntryPoint") == true;

    static AttributeSyntax WithEntryPoint(AttributeSyntax dllImport, string entryPoint)
    {
        var entryPointArgument = SyntaxFactory.AttributeArgument(
            SyntaxFactory.NameEquals(
                SyntaxFactory.IdentifierName("EntryPoint"),
                SyntaxFactory.Token(SyntaxKind.EqualsToken).WithLeadingTrivia(SyntaxFactory.Space).WithTrailingTrivia(SyntaxFactory.Space)),
            nameColon: null,
            SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(entryPoint)));
        var arguments = dllImport.ArgumentList ?? SyntaxFactory.AttributeArgumentList();
        var separator = SyntaxFactory.Token(SyntaxKind.CommaToken).WithTrailingTrivia(SyntaxFactory.Space);
        var separators = arguments.Arguments.GetSeparators().ToList();
        if (arguments.Arguments.Count > 0)
        {
            separators.Add(separator);
        }

        var newArguments = SyntaxFactory.SeparatedList([.. arguments.Arguments, entryPointArgument], separators);
        return dllImport.WithArgumentList(arguments.WithArguments(newArguments));
    }
}
