using RiktigRefactor.CSharp.Tests.Infrastructure;

namespace RiktigRefactor.CSharp.Tests;

public class InvalidIdentifierRenameCaseTests
{
    [Fact]
    [Spec("C# spec §6.4.3 Identifiers")]
    public Task Space()
        => RenameCase.InDirectory("invalid-identifier/space")
            .RenameTo("not valid")
            .ExpectRefusal("invalid-identifier")
            .NaiveShouldFail()
            .RunAsync();
}
