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

    [Fact]
    [Spec("C# spec §15.6.4 Virtual methods")]
    public Task VirtualOverriddenInReferencingProject()
        => RenameCase.InDirectory("multi-project/virtual-overridden-in-referencing-project")
            .RenameTo("Explain")
            .WithProjects(("Lib", []), ("App", ["Lib"]))
            .RunAsync();

    [Fact]
    [Spec("C# spec §18.6.5 Interface mapping")]
    public Task InterfaceImplementedInReferencingProject()
        => RenameCase.InDirectory("multi-project/interface-implemented-in-referencing-project")
            .RenameTo("Size")
            .WithProjects(("Lib", []), ("App", ["Lib"]))
            .RunAsync();

    [Fact]
    [Spec("C# spec §12.8.10 Invocation expressions")]
    public Task CursorInReferencingProject()
        => RenameCase.InDirectory("multi-project/rename-from-referencing-project")
            .RenameTo("Twice")
            .WithProjects(("Lib", []), ("App", ["Lib"]))
            .RunAsync();

    // Derived.Pick hides the renamed Base member, so the call on Derived would bind to Derived.Pick instead.
    [Fact]
    [Spec("C# spec §15.3.5 Hiding through inheritance")]
    public Task HidingMemberInReferencingProject()
        => RenameCase.InDirectory("multi-project/new-hiding-member-in-referencing-project")
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
