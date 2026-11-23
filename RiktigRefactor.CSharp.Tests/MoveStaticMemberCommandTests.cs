using System.Diagnostics;
using System.Text.Json;
using RiktigRefactor.CSharp.Tool;

namespace RiktigRefactor.CSharp.Tests;

/// <summary>Runs the move-static-member command logic against a small restored project on disk.</summary>
public class MoveStaticMemberCommandTests : IDisposable
{
    const string ProjectFile = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <OutputType>Exe</OutputType>
            <TargetFramework>net10.0</TargetFramework>
          </PropertyGroup>
        </Project>
        """;

    const string ProgramSource = "class Order\n{\n    public static int Total(int qty) => 2 * qty;\n\n    static void Main()\n    {\n        System.Console.WriteLine(Order.Total(3));\n    }\n}\n\nclass Pricing\n{\n}\n";

    readonly string _directory = Directory.CreateTempSubdirectory("riktig-cs-").FullName;

    public void Dispose()
    {
        Directory.Delete(this._directory, recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ApplyMovesTheMemberAndUpdatesItsReference()
    {
        WriteProject();
        await RestoreAsync();

        var (exitCode, _, error) = await RunAsync(at: "3:23", target: "Pricing", apply: true, json: false);

        Assert.True(exitCode == RefactoringCommand.ExitSuccess, error);
        var text = File.ReadAllText(PathOf("Program.cs"));
        Assert.Contains("class Pricing\n{\n    public static int Total(int qty) => 2 * qty;\n}", text);
        Assert.Contains("System.Console.WriteLine(Pricing.Total(3));", text);
    }

    [Fact]
    public async Task JsonPrintsTheEditsWithoutWriting()
    {
        WriteProject();
        await RestoreAsync();

        var (exitCode, output, _) = await RunAsync(at: "7:40", target: "Pricing", apply: false, json: true);

        Assert.Equal(RefactoringCommand.ExitSuccess, exitCode);
        using var document = JsonDocument.Parse(output);
        Assert.Equal(3, document.RootElement.GetProperty("edits").GetArrayLength());
        Assert.Equal(ProgramSource, File.ReadAllText(PathOf("Program.cs")));
    }

    [Fact]
    public async Task UnknownTargetIsRefused()
    {
        WriteProject();
        await RestoreAsync();

        var (exitCode, _, error) = await RunAsync(at: "3:23", target: "Nowhere", apply: true, json: false);

        Assert.Equal(RefactoringCommand.ExitRefused, exitCode);
        Assert.Contains("invalid-target", error);
        Assert.Equal(ProgramSource, File.ReadAllText(PathOf("Program.cs")));
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

    async Task<(int ExitCode, string Output, string Error)> RunAsync(string at, string target, bool apply, bool json)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var arguments = new MoveStaticMemberArguments(PathOf("Sample.csproj"), PathOf("Program.cs"), at, target, apply, json);
        var exitCode = await MoveStaticMemberCommand.RunAsync(arguments, output, error, TestContext.Current.CancellationToken);
        return (exitCode, output.ToString(), error.ToString());
    }
}
