using RiktigRefactor.CSharp.Tests.Infrastructure;

namespace RiktigRefactor.CSharp.Tests;

public class OverloadsRenameCaseTests
{
    // After the rename the instance method Box.Look wins over the extension method.
    [Fact]
    [Spec("C# spec §12.8.10.3 Extension method invocations")]
    public Task InstanceMethodHidesExtension()
        => RenameCase.InDirectory("overloads/instance-method-hides-extension")
            .RenameTo("Look")
            .ExpectRefusal("binding-would-change")
            .NaiveShouldFail()
            .RunAsync();

    // After the rename the call would resolve to the better F(int) overload instead of the renamed method.
    // Roslyn repairs this by rewriting the argument as `(short)1`, which the tool does not accept because it changes more than names.
    [Fact]
    [Spec("C# spec §12.6.4 Overload resolution")]
    public Task RenameJoinsOverloadSet()
        => RenameCase.InDirectory("overloads/rename-joins-overload-set")
            .RenameTo("F")
            .ExpectRefusal("binding-would-change")
            .NaiveShouldFail()
            .RunAsync();
}
