using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RiktigRefactor.CSharp.Tests.Infrastructure;

/// <summary>When the project has an additional file named <c>MemberNames.marker</c>, emits a class listing the names of all methods, without mentioning any of them in its own code.</summary>
/// <remarks>Only loaded into test workspaces, never into a real compiler, so the analyzer packaging rules don't apply.</remarks>
#pragma warning disable RS1036, RS1038, RS1041
[Generator]
public class MemberNamesSourceGenerator : IIncrementalGenerator
#pragma warning restore RS1036, RS1038, RS1041
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var methodNames = context.SyntaxProvider
            .CreateSyntaxProvider((node, _) => node is MethodDeclarationSyntax, (syntaxContext, _) => ((MethodDeclarationSyntax)syntaxContext.Node).Identifier.ValueText)
            .Collect();
        var hasMarker = context.AdditionalTextsProvider
            .Where(file => Path.GetFileName(file.Path) == "MemberNames.marker")
            .Collect();
        context.RegisterSourceOutput(methodNames.Combine(hasMarker), (output, pair) =>
        {
            if (pair.Right.Length > 0)
            {
                output.AddSource("MemberNames.g.cs", $"static class MemberNames {{ public const string All = \"{string.Join(",", pair.Left.OrderBy(name => name, StringComparer.Ordinal))}\"; }}");
            }
        });
    }
}
