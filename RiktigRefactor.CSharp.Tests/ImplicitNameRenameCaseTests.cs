using RiktigRefactor.CSharp.Tests.Infrastructure;

namespace RiktigRefactor.CSharp.Tests;

public class ImplicitNameRenameCaseTests
{
    // await using awaits the result of DisposeAsync with GetAwaiter looked up by name, though no await expression appears, so the renamed instance method would capture it from the extension.
    [Fact]
    [Spec("C# spec §13.14 The using statement")]
    public Task AwaitUsingGetAwaiter()
        => RenameCase.InDirectory("implicit-name/await-using-get-awaiter")
            .RenameTo("GetAwaiter")
            .ExpectRefusal("binding-would-change")
            .NaiveShouldFail()
            .RunAsync();

    // Deconstruction looks up Deconstruct by name and prefers an instance method over an extension, so the renamed method would capture it.
    [Fact]
    [Spec("C# spec §12.24.2 Simple assignment")]
    public Task Deconstruct()
        => RenameCase.InDirectory("implicit-name/deconstruct")
            .RenameTo("Deconstruct")
            .ExpectRefusal("binding-would-change")
            .NaiveShouldFail()
            .RunAsync();

    // A using statement on a ref struct calls Dispose by name, so renaming it breaks the using without a visible reference.
    [Fact]
    [Spec("C# spec §13.14 The using statement")]
    public Task Dispose()
        => RenameCase.InDirectory("implicit-name/dispose")
            .RenameTo("Close")
            .ExpectRefusal("binding-would-change")
            .NaiveShouldFailWith("CS1674")
            .RunAsync();

    // await looks up GetAwaiter by name and prefers an instance method over an extension, so the renamed method would capture the await.
    [Fact]
    [Spec("C# spec §12.9.9.2 Awaitable expressions")]
    public Task GetAwaiter()
        => RenameCase.InDirectory("implicit-name/get-awaiter")
            .RenameTo("GetAwaiter")
            .ExpectRefusal("binding-would-change")
            .NaiveShouldFail()
            .RunAsync();

    // foreach looks up GetEnumerator by name and prefers an instance method over an extension, so the renamed method would capture the loop.
    [Fact]
    [Spec("C# spec §13.9.5 The foreach statement")]
    public Task GetEnumerator()
        => RenameCase.InDirectory("implicit-name/get-enumerator")
            .RenameTo("GetEnumerator")
            .ExpectRefusal("binding-would-change")
            .NaiveShouldFail()
            .RunAsync();

    // await calls the awaiter's GetResult by name, so renaming it breaks the await without a visible reference.
    [Fact]
    [Spec("C# spec §12.9.9.4 Run-time evaluation of await expressions")]
    public Task GetResult()
        => RenameCase.InDirectory("implicit-name/get-result")
            .RenameTo("Result")
            .ExpectRefusal("binding-would-change")
            .NaiveShouldFailWith("CS0117")
            .RunAsync();

    // An interpolated string handler's AppendLiteral is called by name, so renaming it breaks the interpolated string without a visible reference.
    [Fact]
    [Spec("C# spec §12.8.3 Interpolated string expressions")]
    public Task InterpolatedStringHandlerAppendLiteral()
        => RenameCase.InDirectory("implicit-name/interpolated-string-handler-append-literal")
            .RenameTo("Literal")
            .ExpectRefusal("binding-would-change")
            .NaiveShouldFail()
            .RunAsync();

    // A list pattern uses a Length property in preference to Count, both looked up by name, so a property renamed to Length would change the pattern's result.
    [Fact]
    [Spec("C# spec §11.2.11 List pattern")]
    public Task ListPatternLength()
        => RenameCase.InDirectory("implicit-name/list-pattern-length")
            .RenameTo("Length")
            .ExpectRefusal("binding-would-change")
            .NaiveShouldFailWith("should use Count")
            .RunAsync();

    // foreach calls Current by name, so renaming it breaks the loop without a visible reference.
    [Fact]
    [Spec("C# spec §13.9.5 The foreach statement")]
    public Task MoveNextCurrent()
        => RenameCase.InDirectory("implicit-name/move-next-current")
            .RenameTo("Value")
            .ExpectRefusal("binding-would-change")
            .NaiveShouldFailWith("CS0117")
            .RunAsync();

    // A query expression calls Select by name and prefers an instance method over an extension, so the renamed method would capture the query.
    [Fact]
    [Spec("C# spec §12.23.3 Query expression translation")]
    public Task QuerySelect()
        => RenameCase.InDirectory("implicit-name/query-select")
            .RenameTo("Select")
            .ExpectRefusal("binding-would-change")
            .NaiveShouldFail()
            .RunAsync();

    // A range index calls Slice by name, so renaming it breaks the range without a visible reference.
    [Fact]
    [Spec("C# spec §18.4 Pattern-based implicit support for Index and Range")]
    public Task RangeSlice()
        => RenameCase.InDirectory("implicit-name/range-slice")
            .RenameTo("Cut")
            .ExpectRefusal("binding-would-change")
            .NaiveShouldFailWith("CS1503")
            .RunAsync();

    // A record uses a user-declared Equals(R) in place of the synthesized one, and == calls it without naming it.
    [Fact]
    [Spec("C# spec §15.16 Record classes")]
    public Task RecordEquals()
        => RenameCase.InDirectory("implicit-name/record-equals")
            .RenameTo("Equals")
            .ExpectRefusal("binding-would-change")
            .NaiveShouldFail()
            .RunAsync();

    // A record's ToString calls a user-declared PrintMembers in place of the synthesized one, without naming it.
    [Fact]
    [Spec("C# spec §15.16 Record classes")]
    public Task RecordPrintMembers()
        => RenameCase.InDirectory("implicit-name/record-print-members")
            .RenameTo("PrintMembers")
            .ExpectRefusal("binding-would-change")
            .NaiveShouldFail()
            .RunAsync();

    // A spread element in a collection expression enumerates with GetEnumerator looked up by name, so the renamed instance method would capture it from the extension.
    [Fact]
    [Spec("C# spec §12.8.25 Collection expressions")]
    public Task SpreadGetEnumerator()
        => RenameCase.InDirectory("implicit-name/spread-get-enumerator")
            .RenameTo("GetEnumerator")
            .ExpectRefusal("binding-would-change")
            .NaiveShouldFail()
            .RunAsync();

    // Bag's collection initializer looks Add up on Bag and its base types, and a Bag.Add(int) would be chosen over Base.Add(object).
    [Fact]
    [Spec("C# spec §12.8.17.2.3 Collection initializers")]
    [Spec("C# spec §12.5 Member lookup")]
    public Task CollectionInitializerOnDerivedType()
        => RenameCase.InDirectory("implicit-name/collection-initializer-on-derived-type")
            .RenameTo("Add")
            .ExpectRefusal("binding-would-change")
            .NaiveShouldFail()
            .RunAsync();

    // Renaming to Add is safe when no collection initializer creates an Inventory: List<int> looks Add up only on itself and its base types.
    [Fact]
    [Spec("C# spec §12.8.17.2.3 Collection initializers")]
    [Spec("C# spec §12.5 Member lookup")]
    public Task UnrelatedCollectionInitializer()
        => RenameCase.InDirectory("implicit-name/unrelated-collection-initializer")
            .RenameTo("Add")
            .RunAsync();

    // Renaming to MoveNext is safe when no foreach enumerates a Counter.
    [Fact]
    [Spec("C# spec §13.9.5 The foreach statement")]
    [Spec("C# spec §12.5 Member lookup")]
    public Task UnrelatedForEach()
        => RenameCase.InDirectory("implicit-name/unrelated-foreach")
            .RenameTo("MoveNext")
            .RunAsync();
}
