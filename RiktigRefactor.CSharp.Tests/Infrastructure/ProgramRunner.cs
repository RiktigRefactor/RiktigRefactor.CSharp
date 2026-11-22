using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace RiktigRefactor.CSharp.Tests.Infrastructure;

/// <summary>Compiles C# source files into a program and runs it, reporting compile errors or the exception it threw.</summary>
public static class ProgramRunner
{
    public static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Latest);

    public static readonly IReadOnlyList<MetadataReference> References = LoadRuntimeReferences();

    /// <returns>Null if the program compiled and ran without throwing; otherwise a description of the failure.</returns>
    public static string? CompileAndRun(IReadOnlyDictionary<string, string> files)
    {
        var trees = files.Select(file => CSharpSyntaxTree.ParseText(file.Value, ParseOptions, file.Key));
        var compilation = CSharpCompilation.Create(
            "TestProgram",
            trees,
            References,
            new CSharpCompilationOptions(OutputKind.ConsoleApplication, nullableContextOptions: NullableContextOptions.Enable));

        using var image = new MemoryStream();
        var emit = compilation.Emit(image);
        if (!emit.Success)
        {
            var errors = emit.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            return "Compile failed:\n" + string.Join("\n", errors);
        }

        image.Position = 0;
        var context = new AssemblyLoadContext("TestProgram", isCollectible: true);
        try
        {
            var entryPoint = context.LoadFromStream(image).EntryPoint
                ?? throw new InvalidOperationException("The program has no entry point.");
            object?[]? arguments = entryPoint.GetParameters().Length == 0 ? null : [Array.Empty<string>()];
            var returned = entryPoint.Invoke(null, arguments);
            if (returned is Task task)
            {
                task.GetAwaiter().GetResult();
            }

            return null;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            return "Program threw:\n" + exception.InnerException;
        }
        finally
        {
            context.Unload();
        }
    }

    static List<MetadataReference> LoadRuntimeReferences()
    {
        var runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var trustedAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        return
        [
            .. trustedAssemblies
                .Where(path => Path.GetDirectoryName(path) == runtimeDirectory)
                .Select(path => MetadataReference.CreateFromFile(path)),
        ];
    }
}
