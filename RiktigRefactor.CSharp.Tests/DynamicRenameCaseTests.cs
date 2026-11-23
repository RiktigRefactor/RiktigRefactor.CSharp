using RiktigRefactor.CSharp.Tests.Infrastructure;

namespace RiktigRefactor.CSharp.Tests;

public class DynamicRenameCaseTests
{
    // The dynamic call binds by name at run time and would throw after the rename.
    [Fact]
    [Spec("C# spec §12.3.3 Dynamic binding")]
    public Task MemberAccessedByName()
        => RenameCase.InDirectory("dynamic/member-accessed-by-name")
            .RenameTo("Greet")
            .ExpectRefusal("dynamic-access-possible")
            .NaiveShouldFail()
            .RunAsync();

    // After the rename an existing dynamic call by the new name would start succeeding.
    [Fact]
    [Spec("C# spec §12.3.3 Dynamic binding")]
    public Task NewNameMatchesDynamicCall()
        => RenameCase.InDirectory("dynamic/new-name-matches-dynamic-call")
            .RenameTo("Greet")
            .ExpectRefusal("dynamic-access-possible")
            .NaiveShouldFail()
            .RunAsync();
}
