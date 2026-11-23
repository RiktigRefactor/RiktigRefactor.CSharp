using RiktigRefactor.CSharp.Tests.Infrastructure;

namespace RiktigRefactor.CSharp.Tests;

public class KeywordRenameCaseTests
{
    // An explicit @ escape makes a keyword a valid identifier; the output keeps the escape.
    [Fact]
    [Spec("C# spec §6.4.3 Identifiers")]
    [Spec("C# spec §6.4.4 Keywords")]
    public Task EscapedKeyword()
        => RenameCase.InDirectory("keyword/escaped-keyword")
            .RenameTo("@class")
            .RunAsync();

    // The tool does not guess that the caller meant @class.
    [Fact]
    [Spec("C# spec §6.4.4 Keywords")]
    public Task UnescapedKeyword()
        => RenameCase.InDirectory("keyword/unescaped-keyword")
            .RenameTo("class")
            .ExpectRefusal("invalid-identifier")
            .NaiveShouldFail()
            .RunAsync();
}
