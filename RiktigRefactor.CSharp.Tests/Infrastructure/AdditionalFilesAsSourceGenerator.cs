using Microsoft.CodeAnalysis;

namespace RiktigRefactor.CSharp.Tests.Infrastructure;

/// <summary>Adds the text of the project's additional files as source, so tests can put any code in generated files.</summary>
/// <remarks>Only loaded into test workspaces, never into a real compiler, so the analyzer packaging rules don't apply.</remarks>
#pragma warning disable RS1036, RS1038, RS1041
[Generator]
public class AdditionalFilesAsSourceGenerator : IIncrementalGenerator
#pragma warning restore RS1036, RS1038, RS1041
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
        => context.RegisterSourceOutput(
            context.AdditionalTextsProvider,
            (output, file) => output.AddSource(Path.GetFileNameWithoutExtension(file.Path) + ".g.cs", file.GetText()!.ToString()));
}
