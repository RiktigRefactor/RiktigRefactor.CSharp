using RiktigRefactor.CSharp.Tests.Infrastructure;

namespace RiktigRefactor.CSharp.Tests;

public class MoveToInstanceMethodCaseTests
{
    // The specification's example; `Discount` is found through `Pricing`, so it is qualified.
    [Fact]
    [Spec("C# spec §15.6.3 Static and instance methods")]
    [Spec("C# spec §12.8.14 This access")]
    public Task SpecExample()
        => MoveToInstanceMethodCase.InDirectory("basic/spec-example")
            .WithReceiver("order")
            .RunAsync();

    // The receiver is the second parameter, and named arguments are written in either order; every argument before the receiver is independent of it.
    [Fact]
    [Spec("C# spec §12.6.2.2 Corresponding parameters")]
    [Spec("C# spec §12.6.2.3 Run-time evaluation of argument lists")]
    public Task ReceiverSecondAndNamed()
        => MoveToInstanceMethodCase.InDirectory("basic/receiver-second-and-named")
            .WithReceiver("order")
            .RunAsync();

    // `Repeat(node, times - 1)` in the body becomes `this.Repeat(times - 1)`.
    [Fact]
    [Spec("C# spec §12.8.10.2 Method invocations")]
    public Task RecursiveCall()
        => MoveToInstanceMethodCase.InDirectory("basic/recursive-call")
            .WithReceiver("node")
            .RunAsync();

    // The method moves to a type in another file; `nameof` and the cref now name it through `Order`.
    [Fact]
    [Spec("C# spec §12.8.23 Nameof expressions")]
    [Spec("C# spec §D.4.2 ID string format")]
    public Task NameofAndOtherFile()
        => MoveToInstanceMethodCase.InDirectory("basic/nameof-and-other-file")
            .WithReceiver("order")
            .RunAsync();

    // `Quantity()` runs before `new Order(...)` today, and would run after it.
    [Fact]
    [Spec("C# spec §12.6.2.3 Run-time evaluation of argument lists")]
    public Task EvaluationOrder()
        => MoveToInstanceMethodCase.InDirectory("refuse/evaluation-order")
            .WithReceiver("order")
            .ExpectRefusal("evaluation-order")
            .NaiveShouldFail()
            .RunAsync();

    // The body never uses `order`, so a null argument works today; an instance call would throw.
    [Fact]
    [Spec("C# spec §12.8.10.2 Method invocations")]
    public Task NullReceiver()
        => MoveToInstanceMethodCase.InDirectory("refuse/null-receiver")
            .WithReceiver("order")
            .ExpectRefusal("receiver-may-be-null")
            .NaiveShouldFail()
            .RunAsync();

    // `special.Label()` would bind to the subclass's method instead of the moved one.
    [Fact]
    [Spec("C# spec §7.7.2.3 Hiding through inheritance")]
    public Task SubclassHasMember()
        => MoveToInstanceMethodCase.InDirectory("refuse/subclass-has-member")
            .WithReceiver("order")
            .ExpectRefusal("name-collision")
            .NaiveShouldFail()
            .RunAsync();

    [Fact]
    [Spec("C# spec §7.7.2.3 Hiding through inheritance")]
    public Task BaseHasMember()
        => MoveToInstanceMethodCase.InDirectory("refuse/base-has-member")
            .WithReceiver("order")
            .ExpectRefusal("name-collision")
            .NaiveShouldFail()
            .RunAsync();

    [Fact]
    [Spec("C# spec §10.8 Method group conversions")]
    public Task MethodGroup()
        => MoveToInstanceMethodCase.InDirectory("refuse/method-group")
            .WithReceiver("order")
            .ExpectRefusal("method-group-reference")
            .NaiveShouldFail()
            .RunAsync();

    [Fact]
    [Spec("C# spec §12.8.14 This access")]
    public Task ReceiverAssigned()
        => MoveToInstanceMethodCase.InDirectory("refuse/receiver-assigned")
            .WithReceiver("order")
            .ExpectRefusal("receiver-written")
            .NaiveShouldFail()
            .RunAsync();

    // The parameter is a copy; `this` in a struct would be the caller's variable.
    [Fact]
    [Spec("C# spec §16.4.5 This access")]
    public Task StructReceiver()
        => MoveToInstanceMethodCase.InDirectory("refuse/struct-receiver")
            .WithReceiver("counter")
            .ExpectRefusal("unsupported-receiver")
            .RunAsync();

    [Fact]
    [Spec("C# spec §10.7.3 Evaluation of lambda expression conversions to expression tree types")]
    public Task ExpressionTree()
        => MoveToInstanceMethodCase.InDirectory("refuse/expression-tree")
            .WithReceiver("order")
            .ExpectRefusal("expression-tree-or-query")
            .NaiveShouldFail()
            .RunAsync();

    [Fact]
    [Spec("C# spec §15.6.2 Method parameters")]
    public Task UnknownParameter()
        => MoveToInstanceMethodCase.InDirectory("refuse/unknown-parameter")
            .WithReceiver("customer")
            .ExpectRefusal("invalid-receiver")
            .RunAsync();
}
