using RiktigRefactor.CSharp.Tests.Infrastructure;

namespace RiktigRefactor.CSharp.Tests;

public class CursorRenameCaseTests
{
    // Console.WriteLine is declared in the framework.
    [Fact]
    public Task FrameworkMember()
        => RenameCase.InDirectory("cursor/framework-member")
            .RenameTo("Print")
            .ExpectRefusal("not-renameable")
            .RunAsync();

    // The cursor is on an empty line.
    [Fact]
    public Task Whitespace()
        => RenameCase.InDirectory("cursor/whitespace")
            .RenameTo("other")
            .ExpectRefusal("no-symbol")
            .RunAsync();
}
