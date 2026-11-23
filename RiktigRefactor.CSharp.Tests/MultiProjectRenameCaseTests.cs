using RiktigRefactor.CSharp.Tests.Infrastructure;

namespace RiktigRefactor.CSharp.Tests;

public class MultiProjectRenameCaseTests
{
    // The renamed member is internal; the friend assembly App sees it through InternalsVisibleTo, so its call would bind to the renamed overload.
    [Fact]
    [Spec("C# spec §22.5.3 Friend assemblies")]
    public Task InternalsVisibleToFriendAssembly()
        => RenameCase.InDirectory("multi-project/internals-visible-to-friend-assembly")
            .RenameTo("Pick")
            .WithProjects(("Lib", []), ("App", ["Lib"]))
            .ExpectRefusal("binding-would-change")
            .NaiveShouldFail()
            .RunAsync();

    // After the rename, a call in the referencing project resolves to the renamed overload instead of the original.
    [Fact]
    [Spec("C# spec §12.6.4 Overload resolution")]
    public Task OverloadInReferencingProject()
        => RenameCase.InDirectory("multi-project/overload-in-referencing-project")
            .RenameTo("Pick")
            .WithProjects(("Lib", []), ("App", ["Lib"]))
            .ExpectRefusal("binding-would-change")
            .NaiveShouldFail()
            .RunAsync();

    // The declaration is in a library project; the only reference is in a project that references it.
    [Fact]
    [Spec("C# spec §12.8.10 Invocation expressions")]
    public Task PublicApiUsedByReferencingProject()
        => RenameCase.InDirectory("multi-project/public-api-used-by-referencing-project")
            .RenameTo("Twice")
            .WithProjects(("Lib", []), ("App", ["Lib"]))
            .RunAsync();
}
