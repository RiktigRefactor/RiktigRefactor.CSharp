using RiktigRefactor.CSharp.Tests.Infrastructure;

namespace RiktigRefactor.CSharp.Tests;

public class RefusalExtractIifeCaseTests
{
    [Fact]
    [Spec("C# spec §12.8.17.3 Anonymous object creation expressions")]
    public Task AnonymousType()
        => ExtractIifeCase.InDirectory("refuse/anonymous-type")
            .ExpectRefusal("type-not-nameable")
            .RunAsync();

    [Fact]
    [Spec("C# spec §12.9.9 Await expressions")]
    public Task AwaitInSelection()
        => ExtractIifeCase.InDirectory("refuse/await-in-selection")
            .ExpectRefusal("contains-await")
            .NaiveShouldFail()
            .RunAsync();

    [Fact]
    [Spec("C# spec §12.8.15 Base access")]
    public Task BaseAccess()
        => ExtractIifeCase.InDirectory("refuse/base-access")
            .ExpectRefusal("base-access")
            .NaiveShouldFail()
            .RunAsync();

    [Fact]
    [Spec("C# spec §13.10.2 The break statement")]
    public Task BreakInSelection()
        => ExtractIifeCase.InDirectory("refuse/break-in-selection")
            .ExpectRefusal("non-local-control-flow")
            .NaiveShouldFail()
            .RunAsync();

    // The lambda stored in the list would capture the static lambda's parameter, a copy, instead of the outer `count`.
    [Fact]
    [Spec("C# spec §12.22.6.2 Captured outer variables")]
    public Task CapturedInsideSelection()
        => ExtractIifeCase.InDirectory("refuse/captured-inside-selection")
            .ExpectRefusal("captured-variable")
            .NaiveShouldFail()
            .RunAsync();

    // `print` reads the outer `n`, which the lambda would only update when it returns, after `print` ran.
    [Fact]
    [Spec("C# spec §12.22.6.2 Captured outer variables")]
    public Task CapturedVariable()
        => ExtractIifeCase.InDirectory("refuse/captured-variable")
            .ExpectRefusal("captured-variable")
            .NaiveShouldFail()
            .RunAsync();

    // The nested lambda would capture the static lambda's parameter, a copy, and miss the later change to `factor`.
    [Fact]
    [Spec("C# spec §12.22.6.2 Captured outer variables")]
    public Task CapturedByEscapingLambda()
        => ExtractIifeCase.InDirectory("refuse/captured-by-escaping-lambda")
            .ExpectRefusal("captured-variable")
            .NaiveShouldFail()
            .RunAsync();

    // Calling the local function reads and writes `total`, which it captures.
    [Fact]
    [Spec("C# spec §12.22.6.2 Captured outer variables")]
    public Task CapturingLocalFunction()
        => ExtractIifeCase.InDirectory("refuse/capturing-local-function")
            .ExpectRefusal("captured-variable")
            .NaiveShouldFail()
            .RunAsync();

    [Fact]
    [Spec("C# spec §10.2.11 Implicit constant expression conversions")]
    public Task ConstantConversion()
        => ExtractIifeCase.InDirectory("refuse/constant-conversion")
            .ExpectRefusal("target-typed-expression")
            .NaiveShouldFail()
            .RunAsync();

    [Fact]
    [Spec("C# spec §9.4 Definite assignment")]
    public Task DeclaredButUnassigned()
        => ExtractIifeCase.InDirectory("refuse/declared-but-unassigned")
            .ExpectRefusal("declaration-escapes")
            .NaiveShouldFail()
            .RunAsync();

    // A lambda call inside an expression tree would become part of the tree.
    [Fact]
    [Spec("C# spec §8.6 Expression tree types")]
    public Task ExpressionTree()
        => ExtractIifeCase.InDirectory("refuse/expression-tree")
            .ExpectRefusal("expression-tree-or-query")
            .NaiveShouldFail()
            .RunAsync();

    // `x++` would increment the lambda's copy, and the `x` after it would read the old value.
    [Fact]
    [Spec("C# spec §12.8.16 Postfix increment and decrement operators")]
    public Task ExpressionWritesVariable()
        => ExtractIifeCase.InDirectory("refuse/expression-writes-variable")
            .ExpectRefusal("expression-writes-variable")
            .NaiveShouldFail()
            .RunAsync();

    [Fact]
    [Spec("C# spec §13.5 Labeled statements")]
    [Spec("C# spec §13.10.4 The goto statement")]
    public Task GotoLabel()
        => ExtractIifeCase.InDirectory("refuse/goto-label")
            .ExpectRefusal("non-local-control-flow")
            .RunAsync();

    // As a string, the lambda's result could not convert to FormattableString.
    [Fact]
    [Spec("C# spec §10.2.5 Implicit interpolated string conversions")]
    public Task InterpolatedStringTargetTyped()
        => ExtractIifeCase.InDirectory("refuse/interpolated-string-target-typed")
            .ExpectRefusal("target-typed-expression")
            .NaiveShouldFail()
            .RunAsync();

    [Fact]
    [Spec("C# spec §12.22 Anonymous function expressions")]
    public Task LambdaTargetTyped()
        => ExtractIifeCase.InDirectory("refuse/lambda-target-typed")
            .ExpectRefusal("target-typed-expression")
            .RunAsync();

    [Fact]
    public Task PartialToken()
        => ExtractIifeCase.InDirectory("refuse/partial-token")
            .ExpectRefusal("invalid-selection")
            .RunAsync();

    [Fact]
    [Spec("C# spec §6.5.5 Conditional compilation directives")]
    public Task PreprocessorDirective()
        => ExtractIifeCase.InDirectory("refuse/preprocessor-directive")
            .ExpectRefusal("preprocessor-region")
            .RunAsync();

    // The lambda would copy `first` on entry and not see the write through the array.
    // `count` is shared with Bump like a field, so the lambda's copy would miss Bump's increment.
    [Fact]
    [Spec("C# spec §15.11.6 Primary constructors")]
    public Task PrimaryConstructorParameter()
        => ExtractIifeCase.InDirectory("refuse/primary-constructor-parameter")
            .ExpectRefusal("captured-variable")
            .NaiveShouldFail()
            .RunAsync();

    [Fact]
    [Spec("C# spec §13.6.2.4 Explicitly typed ref local variable declarations")]
    public Task RefLocal()
        => ExtractIifeCase.InDirectory("refuse/ref-local")
            .ExpectRefusal("ref-or-pointer-across-boundary")
            .NaiveShouldFail()
            .RunAsync();

    [Fact]
    [Spec("C# spec §13.10.5 The return statement")]
    public Task ReturnInSelection()
        => ExtractIifeCase.InDirectory("refuse/return-in-selection")
            .ExpectRefusal("non-local-control-flow")
            .NaiveShouldFail()
            .RunAsync();

    // Passing `this` of a struct copies it, so the increments would be lost.
    [Fact]
    [Spec("C# spec §12.8.14 This access")]
    [Spec("C# spec §16.8 Class and struct differences")]
    public Task StructThis()
        => ExtractIifeCase.InDirectory("refuse/struct-this")
            .ExpectRefusal("struct-this")
            .NaiveShouldFail()
            .RunAsync();

    // Move would run on the copy the lambda returns.
    [Fact]
    [Spec("C# spec §12.2 Expression classifications")]
    public Task StructVariableReceiver()
        => ExtractIifeCase.InDirectory("refuse/struct-variable-receiver")
            .ExpectRefusal("selection-is-variable")
            .NaiveShouldFail()
            .RunAsync();

    // The resource would be disposed when the lambda returns, before "after" is logged.
    [Fact]
    [Spec("C# spec §13.14 The using statement")]
    public Task UsingDeclaration()
        => ExtractIifeCase.InDirectory("refuse/using-declaration")
            .ExpectRefusal("disposal-would-move")
            .NaiveShouldFail()
            .RunAsync();

    // The lambda returns `step` only when it completes, so the exception would lose the assignment the catch block's code relies on.
    [Fact]
    [Spec("C# spec §13.11 The try statement")]
    public Task WritesLostOnException()
        => ExtractIifeCase.InDirectory("refuse/writes-lost-on-exception")
            .ExpectRefusal("writes-lost-on-exception")
            .NaiveShouldFail()
            .RunAsync();
}
