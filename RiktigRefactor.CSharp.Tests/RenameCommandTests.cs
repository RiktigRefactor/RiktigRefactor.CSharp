using System.Text.Json;
using RiktigRefactor.CSharp.Tool;

namespace RiktigRefactor.CSharp.Tests;

/// <summary>Runs the CLI logic against a small project on disk, which exercises MSBuild workspace loading.</summary>
public class RenameCommandTests : IDisposable
{
    const string ProjectFile = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <OutputType>Exe</OutputType>
            <TargetFramework>net10.0</TargetFramework>
          </PropertyGroup>
        </Project>
        """;

    const string CalculatorSource = "static class Calculator\n{\n    public static int Double(int value) => value * 2;\n}\n";

    const string ProgramSource = "class Program\n{\n    static void Main()\n    {\n        System.Console.WriteLine(Calculator.Double(21));\n    }\n}\n";

    readonly string _directory = Directory.CreateTempSubdirectory("riktig-cs-").FullName;

    public void Dispose()
    {
        Directory.Delete(this._directory, recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task PreviewPrintsDiffAndLeavesFilesAlone()
    {
        WriteProject();

        var (exitCode, output, _) = await RunAsync(newName: "Twice", apply: false, json: false);

        Assert.Equal(RenameCommand.ExitSuccess, exitCode);
        Assert.Contains("-    public static int Double(int value) => value * 2;", output);
        Assert.Contains("+    public static int Twice(int value) => value * 2;", output);
        Assert.Contains("+        System.Console.WriteLine(Calculator.Twice(21));", output);
        Assert.Equal(CalculatorSource, File.ReadAllText(PathOf("Calculator.cs")));
    }

    [Fact]
    public async Task ApplyWritesFilesAcrossTheProject()
    {
        WriteProject();

        var (exitCode, _, _) = await RunAsync(newName: "Twice", apply: true, json: false);

        Assert.Equal(RenameCommand.ExitSuccess, exitCode);
        Assert.Equal(CalculatorSource.Replace("Double", "Twice", StringComparison.Ordinal), File.ReadAllText(PathOf("Calculator.cs")));
        Assert.Equal(ProgramSource.Replace("Double", "Twice", StringComparison.Ordinal), File.ReadAllText(PathOf("Program.cs")));
    }

    [Fact]
    public async Task JsonPrintsEditsWithoutSolution()
    {
        WriteProject();

        var (exitCode, output, _) = await RunAsync(newName: "Twice", apply: false, json: true);

        Assert.Equal(RenameCommand.ExitSuccess, exitCode);
        using var document = JsonDocument.Parse(output);
        Assert.Equal("success", document.RootElement.GetProperty("status").GetString());
        var edits = document.RootElement.GetProperty("edits");
        Assert.Equal(2, edits.GetArrayLength());
        Assert.Equal("Twice", edits[0].GetProperty("newText").GetString());
        Assert.Equal(3, edits[0].GetProperty("start").GetProperty("line").GetInt32());
        Assert.False(document.RootElement.TryGetProperty("newSolution", out _));
    }

    [Fact]
    public async Task RefusalExitsWithOneAndWritesNothing()
    {
        WriteProject();

        var (exitCode, _, error) = await RunAsync(newName: "class", apply: true, json: false);

        Assert.Equal(RenameCommand.ExitRefused, exitCode);
        Assert.Contains("invalid-identifier", error);
        Assert.Equal(CalculatorSource, File.ReadAllText(PathOf("Calculator.cs")));
    }

    [Fact]
    public async Task UnknownFileExitsWithTwo()
    {
        WriteProject();

        var (exitCode, _, error) = await RunAsync(newName: "Twice", apply: false, json: false, file: "Missing.cs");

        Assert.Equal(RenameCommand.ExitError, exitCode);
        Assert.Contains("not part of the workspace", error);
    }

    string PathOf(string name) => Path.Combine(this._directory, name);

    void WriteProject()
    {
        File.WriteAllText(PathOf("Sample.csproj"), ProjectFile);
        File.WriteAllText(PathOf("Calculator.cs"), CalculatorSource);
        File.WriteAllText(PathOf("Program.cs"), ProgramSource);
    }

    async Task<(int ExitCode, string Output, string Error)> RunAsync(string newName, bool apply, bool json, string file = "Calculator.cs")
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var arguments = new RenameArguments(PathOf("Sample.csproj"), PathOf(file), "3:23", newName, apply, json);
        var exitCode = await RenameCommand.RunAsync(arguments, output, error, TestContext.Current.CancellationToken);
        return (exitCode, output.ToString(), error.ToString());
    }
}
