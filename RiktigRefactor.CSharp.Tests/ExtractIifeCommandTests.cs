using System.Diagnostics;
using System.Text.Json;
using RiktigRefactor.CSharp.Tool;

namespace RiktigRefactor.CSharp.Tests;

/// <summary>
/// Runs the extract-iife command logic against a small project on disk.
/// Unlike rename, extraction needs the types of expressions, so the project is restored first; without that, every type is unknown and the tool refuses.
/// </summary>
public class ExtractIifeCommandTests : IDisposable
{
    const string ProjectFile = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <OutputType>Exe</OutputType>
            <TargetFramework>net10.0</TargetFramework>
          </PropertyGroup>
        </Project>
        """;

    const string ProgramSource = "class Program\n{\n    static void Main()\n    {\n        var a = 20;\n        var total = a * 2 + 2;\n        System.Console.WriteLine(total);\n    }\n}\n";

    readonly string _directory = Directory.CreateTempSubdirectory("riktig-cs-").FullName;

    public void Dispose()
    {
        Directory.Delete(this._directory, recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ApplyWritesTheLambda()
    {
        WriteProject();
        await RestoreAsync();

        var (exitCode, _, _) = await RunAsync(from: "6:21", to: "6:26", apply: true, json: false);

        Assert.Equal(RefactoringCommand.ExitSuccess, exitCode);
        Assert.Contains("var total = ((System.Func<int, int>)(static (a) => a * 2))(a) + 2;", File.ReadAllText(PathOf("Program.cs")));
    }

    [Fact]
    public async Task JsonPrintsOneEdit()
    {
        WriteProject();
        await RestoreAsync();

        var (exitCode, output, _) = await RunAsync(from: "6:21", to: "6:26", apply: false, json: true);

        Assert.Equal(RefactoringCommand.ExitSuccess, exitCode);
        using var document = JsonDocument.Parse(output);
        var edit = Assert.Single(document.RootElement.GetProperty("edits").EnumerateArray());
        Assert.Equal("Program.cs", edit.GetProperty("file").GetString());
        Assert.Equal(ProgramSource, File.ReadAllText(PathOf("Program.cs")));
    }

    [Fact]
    public async Task PartialTokenIsRefused()
    {
        WriteProject();

        var (exitCode, _, error) = await RunAsync(from: "6:22", to: "6:26", apply: true, json: false);

        Assert.Equal(RefactoringCommand.ExitRefused, exitCode);
        Assert.Contains("invalid-selection", error);
        Assert.Equal(ProgramSource, File.ReadAllText(PathOf("Program.cs")));
    }

    [Fact]
    public async Task EndBeforeStartExitsWithTwo()
    {
        WriteProject();

        var (exitCode, _, error) = await RunAsync(from: "6:26", to: "6:21", apply: false, json: false);

        Assert.Equal(RefactoringCommand.ExitError, exitCode);
        Assert.Contains("is before", error);
    }

    string PathOf(string name) => Path.Combine(this._directory, name);

    void WriteProject()
    {
        File.WriteAllText(PathOf("Sample.csproj"), ProjectFile);
        File.WriteAllText(PathOf("Program.cs"), ProgramSource);
    }

    async Task RestoreAsync()
    {
        var dotnet = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet";
        var startInfo = new ProcessStartInfo(dotnet, ["restore", PathOf("Sample.csproj")])
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var restore = Process.Start(startInfo)!;
        var output = await restore.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        await restore.WaitForExitAsync(TestContext.Current.CancellationToken);
        Assert.True(restore.ExitCode == 0, output);
    }

    async Task<(int ExitCode, string Output, string Error)> RunAsync(string from, string to, bool apply, bool json)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var arguments = new ExtractIifeArguments(PathOf("Sample.csproj"), PathOf("Program.cs"), from, to, apply, json);
        var exitCode = await ExtractIifeCommand.RunAsync(arguments, output, error, TestContext.Current.CancellationToken);
        return (exitCode, output.ToString(), error.ToString());
    }
}
