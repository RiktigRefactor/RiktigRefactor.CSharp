using RiktigRefactor.CSharp.Tests.Infrastructure;

namespace RiktigRefactor.CSharp.Tests;

public class PrimaryConstructorRenameCaseTests
{
    // A field named like the primary constructor parameter hides it in member bodies.
    [Fact]
    [Spec("C# spec §15.11.6 Primary constructors")]
    public Task FieldRenamedToParameterName()
        => RenameCase.InDirectory("primary-constructor/field-renamed-to-parameter-name")
            .RenameTo("balance")
            .ExpectRefusal("binding-would-change")
            .NaiveShouldFail()
            .RunAsync();

    // In a member body, a field of the same name hides the primary constructor parameter.
    [Fact]
    [Spec("C# spec §15.11.6 Primary constructors")]
    public Task ParameterRenamedToFieldName()
        => RenameCase.InDirectory("primary-constructor/parameter-renamed-to-field-name")
            .RenameTo("total")
            .ExpectRefusal("binding-would-change")
            .NaiveShouldFail()
            .RunAsync();
}
