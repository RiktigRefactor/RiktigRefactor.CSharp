using RiktigRefactor.CSharp.Tests.Infrastructure;

namespace RiktigRefactor.CSharp.Tests;

public class PartialRenameCaseTests
{
    [Fact]
    [Spec("C# spec §15.2.7 Partial type declarations")]
    public Task MethodAcrossFiles()
        => RenameCase.InDirectory("partial/method-across-files")
            .RenameTo("Compute")
            .RunAsync();

    // Renaming the implementing declaration renames the defining one too.
    [Fact]
    [Spec("C# spec §15.6.9 Partial methods")]
    public Task MethodDefinitionAndImplementation()
        => RenameCase.InDirectory("partial/method-definition-and-implementation")
            .RenameTo("Compute")
            .RunAsync();

    // Every part of the partial type is renamed.
    [Fact]
    [Spec("C# spec §15.2.7 Partial type declarations")]
    public Task Type()
        => RenameCase.InDirectory("partial/type")
            .RenameTo("Gadget")
            .RunAsync();
}
