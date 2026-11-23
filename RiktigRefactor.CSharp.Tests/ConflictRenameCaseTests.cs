using RiktigRefactor.CSharp.Tests.Infrastructure;

namespace RiktigRefactor.CSharp.Tests;

public class ConflictRenameCaseTests
{
    [Fact]
    [Spec("C# spec §7.6 Scopes")]
    public Task LocalAlreadyInScope()
        => RenameCase.InDirectory("conflict/local-already-in-scope")
            .RenameTo("b")
            .ExpectRefusal("conflicting-declaration")
            .NaiveShouldFail()
            .RunAsync();

    [Fact]
    [Spec("C# spec §15.6 Methods")]
    public Task MethodWithSameSignature()
        => RenameCase.InDirectory("conflict/method-with-same-signature")
            .RenameTo("B")
            .ExpectRefusal("conflicting-declaration")
            .NaiveShouldFail()
            .RunAsync();

    [Fact]
    [Spec("C# spec §15.6.2 Method parameters")]
    public Task ParameterNameAlreadyUsed()
        => RenameCase.InDirectory("conflict/parameter-name-already-used")
            .RenameTo("right")
            .ExpectRefusal("conflicting-declaration")
            .NaiveShouldFail()
            .RunAsync();
}
