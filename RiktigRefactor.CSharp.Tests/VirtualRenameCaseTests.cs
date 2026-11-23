using RiktigRefactor.CSharp.Tests.Infrastructure;

namespace RiktigRefactor.CSharp.Tests;

public class VirtualRenameCaseTests
{
    // The overridden member is declared in the framework; renaming the override would silently stop overriding.
    [Fact]
    [Spec("C# spec §15.6.5 Override methods")]
    public Task OverrideOfFrameworkMember()
        => RenameCase.InDirectory("virtual/override-of-framework-member")
            .RenameTo("Describe")
            .ExpectRefusal("not-renameable")
            .NaiveShouldFail()
            .RunAsync();

    [Fact]
    [Spec("C# spec §15.6.4 Virtual methods")]
    [Spec("C# spec §15.6.5 Override methods")]
    public Task RenameFromBaseVirtual()
        => RenameCase.InDirectory("virtual/rename-from-base-virtual")
            .RenameTo("MakeNoise")
            .RunAsync();

    // Renaming a leaf override renames the whole hierarchy.
    [Fact]
    [Spec("C# spec §15.6.5 Override methods")]
    public Task RenameFromOverride()
        => RenameCase.InDirectory("virtual/rename-from-override")
            .RenameTo("Title")
            .RunAsync();
}
