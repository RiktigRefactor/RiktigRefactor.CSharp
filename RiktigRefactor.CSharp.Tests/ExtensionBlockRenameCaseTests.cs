using RiktigRefactor.CSharp.Tests.Infrastructure;

namespace RiktigRefactor.CSharp.Tests;

public class ExtensionBlockRenameCaseTests
{
    // An instance method takes precedence over an extension member of the same name, so the renamed extension would lose its call sites.
    [Fact]
    [Spec("C# 14 extension members")]
    public Task CapturedByInstanceMethod()
        => RenameCase.InDirectory("extension-block/captured-by-instance-method")
            .RenameTo("Twice")
            .ExpectRefusal("binding-would-change")
            .NaiveShouldFail()
            .RunAsync();

    [Fact]
    [Spec("C# 14 extension members")]
    public Task Method()
        => RenameCase.InDirectory("extension-block/method")
            .RenameTo("Doubled")
            .RunAsync();

    [Fact]
    [Spec("C# 14 extension members")]
    public Task Property()
        => RenameCase.InDirectory("extension-block/property")
            .RenameTo("Doubled")
            .RunAsync();

    [Fact]
    [Spec("C# 14 extension members")]
    public Task ReceiverParameter()
        => RenameCase.InDirectory("extension-block/receiver-parameter")
            .RenameTo("value")
            .RunAsync();
}
