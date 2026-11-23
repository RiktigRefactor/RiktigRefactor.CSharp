using RiktigRefactor.CSharp.Tests.Infrastructure;

namespace RiktigRefactor.CSharp.Tests;

public class TopLevelRenameCaseTests
{
    [Fact]
    [Spec("C# spec §7.1.3 Using top-level statements")]
    public Task Local()
        => RenameCase.InDirectory("top-level/local")
            .RenameTo("Total")
            .RunAsync();

    // A top-level local function shadows a type of the same name, so the type must be qualified with global::.
    [Fact]
    [Spec("C# spec §7.1.3 Using top-level statements")]
    public Task LocalFunctionRenamedToTypeName()
        => RenameCase.InDirectory("top-level/local-function-renamed-to-type-name")
            .RenameTo("C")
            .NaiveShouldFail()
            .RunAsync();
}
