using RiktigRefactor.CSharp.Tests.Infrastructure;

namespace RiktigRefactor.CSharp.Tests;

public class BasicRenameCaseTests
{
    // The cursor is on the declaration; the only reference is in another file.
    [Fact]
    [Spec("C# spec §12.8.10 Invocation expressions")]
    public Task CrossFileMethod()
        => RenameCase.InDirectory("basic/cross-file-method")
            .RenameTo("Twice")
            .RunAsync();

    // A cref in a documentation comment is a reference and is renamed with the symbol.
    // Comment text is not.
    [Fact]
    [Spec("C# spec §D Documentation comments")]
    public Task DocCommentCref()
        => RenameCase.InDirectory("basic/doc-comment-cref")
            .RenameTo("Compute")
            .RunAsync();

    [Fact]
    [Spec("C# spec §20.4 Enum members")]
    public Task EnumMember()
        => RenameCase.InDirectory("basic/enum-member")
            .RenameTo("Lime")
            .RunAsync();

    [Fact]
    [Spec("C# spec §15.6.10 Extension methods")]
    public Task ExtensionMethod()
        => RenameCase.InDirectory("basic/extension-method")
            .RenameTo("Doubled")
            .RunAsync();

    // An anonymous type has no name, so the verifier must match it by position and member names rather than by name.
    [Fact]
    [Spec("C# spec §12.8.17.3 Anonymous object creation expressions")]
    public Task FileWithAnonymousTypeLocal()
        => RenameCase.InDirectory("basic/file-with-anonymous-type-local")
            .RenameTo("Compute")
            .RunAsync();

    [Fact]
    [Spec("C# spec §15.2.3 Type parameters")]
    public Task GenericMethodTypeParameter()
        => RenameCase.InDirectory("basic/generic-method-type-parameter")
            .RenameTo("TItem")
            .RunAsync();

    [Fact]
    [Spec("C# spec §13.5 Labeled statements")]
    public Task GotoLabel()
        => RenameCase.InDirectory("basic/goto-label")
            .RenameTo("Again")
            .RunAsync();

    [Fact]
    [Spec("C# spec §12.22 Anonymous function expressions")]
    public Task LambdaParameter()
        => RenameCase.InDirectory("basic/lambda-parameter")
            .RenameTo("x")
            .RunAsync();

    // Simple rename of a local from its declaration.
    [Fact]
    [Spec("C# spec §7.6 Scopes")]
    public Task LocalVariable()
        => RenameCase.InDirectory("basic/local-variable")
            .RenameTo("sum")
            .RunAsync();

    // The named argument at the call site must be renamed with the parameter; if it were not, the call would not compile.
    [Fact]
    [Spec("C# spec §12.6.2 Argument lists")]
    public Task NamedArgument()
        => RenameCase.InDirectory("basic/named-argument")
            .RenameTo("start")
            .RunAsync();

    // nameof tracks the rename, so the reflection lookup by the nameof string still finds the property.
    [Fact]
    [Spec("C# spec §12.8.23 The nameof operator")]
    public Task Nameof()
        => RenameCase.InDirectory("basic/nameof")
            .RenameTo("Quantity")
            .RunAsync();

    [Fact]
    [Spec("C# spec §14 Namespaces")]
    public Task Namespace()
        => RenameCase.InDirectory("basic/namespace")
            .RenameTo("Forms")
            .RunAsync();

    [Fact]
    [Spec("C# spec §15.7 Properties")]
    [Spec("C# spec §11.2.6 Property pattern")]
    public Task PropertyInitializerAndPattern()
        => RenameCase.InDirectory("basic/property-initializer-and-pattern")
            .RenameTo("FullName")
            .RunAsync();

    [Fact]
    [Spec("C# spec §12.23 Query expressions")]
    public Task QueryRangeVariable()
        => RenameCase.InDirectory("basic/query-range-variable")
            .RenameTo("n")
            .RunAsync();

    // The positional parameter and the property it generates are the same symbol for rename purposes.
    // The record's synthesized ToString now prints the new name, which is intended (see the spec's limitations.md).
    [Fact]
    [Spec("C# spec §15.16 Record classes")]
    public Task RecordPositionalParameter()
        => RenameCase.InDirectory("basic/record-positional-parameter")
            .RenameTo("Horizontal")
            .RunAsync();

    // Renaming from a constructor call renames the type.
    [Fact]
    [Spec("C# spec §12.8.17.2 Object creation expressions")]
    public Task TypeViaConstructorCall()
        => RenameCase.InDirectory("basic/type-via-constructor-call")
            .RenameTo("Gadget")
            .RunAsync();

    // Renaming from a constructor declaration renames the type.
    [Fact]
    [Spec("C# spec §15.11 Instance constructors")]
    public Task TypeViaConstructorDeclaration()
        => RenameCase.InDirectory("basic/type-via-constructor-declaration")
            .RenameTo("Gadget")
            .RunAsync();

    [Fact]
    [Spec("C# spec §14.6.2 Using alias directives")]
    public Task UsingAlias()
        => RenameCase.InDirectory("basic/using-alias")
            .RenameTo("Numbers")
            .RunAsync();
}
