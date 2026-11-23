using RiktigRefactor.CSharp.Tests.Infrastructure;

namespace RiktigRefactor.CSharp.Tests;

public class InterfaceRenameCaseTests
{
    [Fact]
    [Spec("C# spec §19.6.2 Explicit interface member implementations")]
    public Task ExplicitImplementation()
        => RenameCase.InDirectory("interface/explicit-implementation")
            .RenameTo("Measure")
            .RunAsync();

    // The member implements IDisposable.Dispose, which is declared outside the workspace and cannot be renamed.
    [Fact]
    [Spec("C# spec §19.6 Interface implementations")]
    public Task ImplementsFrameworkInterface()
        => RenameCase.InDirectory("interface/implements-framework-interface")
            .RenameTo("Release")
            .ExpectRefusal("not-renameable")
            .NaiveShouldFail()
            .RunAsync();

    // Renaming the implementation also renames the interface member, otherwise the class would stop implementing it.
    [Fact]
    [Spec("C# spec §19.6 Interface implementations")]
    public Task RenameFromImplementation()
        => RenameCase.InDirectory("interface/rename-from-implementation")
            .RenameTo("Measure")
            .RunAsync();

    // The implicit implementation must be renamed together with the interface member.
    [Fact]
    [Spec("C# spec §19.6 Interface implementations")]
    public Task RenameFromInterfaceMember()
        => RenameCase.InDirectory("interface/rename-from-interface-member")
            .RenameTo("Measure")
            .RunAsync();
}
