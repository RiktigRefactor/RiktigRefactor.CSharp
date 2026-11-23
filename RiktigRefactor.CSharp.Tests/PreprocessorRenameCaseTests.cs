using RiktigRefactor.CSharp.Tests.Infrastructure;

namespace RiktigRefactor.CSharp.Tests;

public class PreprocessorRenameCaseTests
{
    // The disabled region uses the name, and it would not be renamed or verified.
    // Not an edge case for the naive check: the program is only wrong when NEVER_DEFINED is defined.
    [Fact]
    [Spec("C# spec §6.5.5 Conditional compilation directives")]
    public Task DisabledRegionMentionsName()
        => RenameCase.InDirectory("preprocessor/disabled-region-mentions-name")
            .RenameTo("counter")
            .ExpectRefusal("preprocessor-region")
            .RunAsync();
}
