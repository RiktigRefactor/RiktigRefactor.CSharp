using RiktigRefactor.CSharp.Tests.Infrastructure;

namespace RiktigRefactor.CSharp.Tests;

public class CallerInfoRenameCaseTests
{
    // The attribute names the parameter in a string, which Roslyn does not update.
    // The expression would silently stop being captured.
    [Fact]
    [Spec("C# spec §23.5.6 Caller-info attributes")]
    public Task CallerArgumentExpressionParameterName()
        => RenameCase.InDirectory("caller-info/caller-argument-expression-parameter-name")
            .RenameTo("candidate")
            .ExpectRefusal("string-names-symbol")
            .NaiveShouldFail()
            .RunAsync();

    // The compiler recomputes the caller name, so the reflection lookup keeps working.
    // Like nameof, the string changes by design.
    [Fact]
    [Spec("C# spec §23.5.6 Caller-info attributes")]
    public Task CallerMemberName()
        => RenameCase.InDirectory("caller-info/caller-member-name")
            .RenameTo("Invoke")
            .RunAsync();
}
