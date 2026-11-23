using RiktigRefactor.CSharp.Tests.Infrastructure;

namespace RiktigRefactor.CSharp.Tests;

public class MakeMethodStaticCaseTests
{
    [Fact]
    [Spec("C# spec §15.6.3 Static and instance methods")]
    public Task ExpressionBodied()
        => MakeMethodStaticCase.InDirectory("basic/expression-bodied")
            .RunAsync();

    // Unqualified fields, properties and calls, `this` as a value, and uses inside a lambda and a local function all go through the receiver.
    [Fact]
    [Spec("C# spec §12.8.14 This access")]
    [Spec("C# spec §12.8.4 Simple names")]
    public Task ImplicitReceiverUses()
        => MakeMethodStaticCase.InDirectory("basic/implicit-receiver-uses")
            .RunAsync();

    // Unqualified calls, including the method's recursive call to itself, pass `this` or the receiver parameter.
    [Fact]
    [Spec("C# spec §12.8.10.2 Method invocations")]
    public Task CallsInsideType()
        => MakeMethodStaticCase.InDirectory("basic/calls-inside-type")
            .RunAsync();

    // `base.Greet(...)` from a subclass calls the same non-virtual method, so it becomes `Animal.Greet(this, ...)`.
    [Fact]
    [Spec("C# spec §12.8.15 Base access")]
    public Task CallsFromSubclass()
        => MakeMethodStaticCase.InDirectory("basic/calls-from-subclass")
            .RunAsync();

    [Fact]
    [Spec("C# spec §15.6.2 Method parameters")]
    public Task ReceiverName()
        => MakeMethodStaticCase.InDirectory("basic/receiver-name")
            .WithReceiverName("self")
            .RunAsync();

    // The call site names the constructed type `Box<int>`, and the method's type argument is still inferred or written.
    [Fact]
    [Spec("C# spec §12.6.3 Type inference")]
    public Task GenericTypeAndMethod()
        => MakeMethodStaticCase.InDirectory("basic/generic-type-and-method")
            .RunAsync();

    [Fact]
    [Spec("C# spec §12.8.23 Nameof expressions")]
    [Spec("C# spec §D.4.2 ID string format")]
    public Task NameofAndCref()
        => MakeMethodStaticCase.InDirectory("basic/nameof-and-cref")
            .RunAsync();

    // A mutating struct method takes its receiver by `ref`, so calls on locals, array elements and `this` still mutate them.
    [Fact]
    [Spec("C# spec §16.4.5 This access")]
    [Spec("C# spec §15.6.2.3.3 Reference parameters")]
    public Task StructMutatingLocal()
        => MakeMethodStaticCase.InDirectory("struct/mutating-local")
            .RunAsync();

    // A readonly method takes its receiver by `in`, so readonly fields and temporaries are still allowed.
    [Fact]
    [Spec("C# spec §15.6.2.3.2 Input parameters")]
    public Task StructReadonlyMethod()
        => MakeMethodStaticCase.InDirectory("struct/readonly-method")
            .RunAsync();

    // `order.Total(Quantity())` evaluates the argument and then throws; a static method would run the body first.
    [Fact]
    [Spec("C# spec §12.8.10.2 Method invocations")]
    public Task NullReceiverSideEffect()
        => MakeMethodStaticCase.InDirectory("refuse/null-receiver-side-effect")
            .ExpectRefusal("receiver-may-be-null")
            .NaiveShouldFail()
            .RunAsync();

    [Fact]
    [Spec("C# spec §12.8.8 Null conditional member access")]
    public Task NullConditional()
        => MakeMethodStaticCase.InDirectory("refuse/null-conditional")
            .ExpectRefusal("receiver-may-be-null")
            .NaiveShouldFail()
            .RunAsync();

    [Fact]
    [Spec("C# spec §15.6.4 Virtual methods")]
    public Task Virtual()
        => MakeMethodStaticCase.InDirectory("refuse/virtual")
            .ExpectRefusal("dynamic-dispatch")
            .NaiveShouldFail()
            .RunAsync();

    // `Base.Name` implements `INamed.Name` for `Derived`, which declares nothing itself.
    [Fact]
    [Spec("C# spec §19.6.5 Interface mapping")]
    public Task InterfaceImplementedBySubclass()
        => MakeMethodStaticCase.InDirectory("refuse/interface-implemented-by-subclass")
            .ExpectRefusal("dynamic-dispatch")
            .NaiveShouldFail()
            .RunAsync();

    [Fact]
    [Spec("C# spec §10.8 Method group conversions")]
    public Task MethodGroup()
        => MakeMethodStaticCase.InDirectory("refuse/method-group")
            .ExpectRefusal("method-group-reference")
            .NaiveShouldFail()
            .RunAsync();

    // The call ran on a copy of the readonly field; `ref` would not compile, and would mutate the field if it did.
    [Fact]
    [Spec("C# spec §12.8.10.2 Method invocations")]
    [Spec("C# spec §15.5.3 Readonly fields")]
    public Task StructReadonlyField()
        => MakeMethodStaticCase.InDirectory("refuse/struct-readonly-field")
            .ExpectRefusal("receiver-not-variable")
            .NaiveShouldFail()
            .RunAsync();

    [Fact]
    [Spec("C# spec §12.8.10.2 Method invocations")]
    public Task StructTemporary()
        => MakeMethodStaticCase.InDirectory("refuse/struct-temporary")
            .ExpectRefusal("receiver-not-variable")
            .NaiveShouldFail()
            .RunAsync();

    // `Order.Total(order, 3)` binds to `Total(object, int)` today, and would bind to the new static overload.
    [Fact]
    [Spec("C# spec §12.6.4 Overload resolution")]
    public Task OverloadCaptured()
        => MakeMethodStaticCase.InDirectory("refuse/overload-captured")
            .ExpectRefusal("binding-would-change")
            .NaiveShouldFail()
            .RunAsync();

    [Fact]
    [Spec("C# spec §15.6.1 General")]
    public Task ConflictingOverload()
        => MakeMethodStaticCase.InDirectory("refuse/conflicting-overload")
            .ExpectRefusal("conflicting-declaration")
            .NaiveShouldFail()
            .RunAsync();

    [Fact]
    [Spec("C# spec §12.8.15 Base access")]
    public Task BaseInBody()
        => MakeMethodStaticCase.InDirectory("refuse/base-in-body")
            .ExpectRefusal("base-access")
            .NaiveShouldFail()
            .RunAsync();

    [Fact]
    [Spec("C# spec §10.7.3 Evaluation of lambda expression conversions to expression tree types")]
    public Task ExpressionTree()
        => MakeMethodStaticCase.InDirectory("refuse/expression-tree")
            .ExpectRefusal("expression-tree-or-query")
            .NaiveShouldFail()
            .RunAsync();

    [Fact]
    [Spec("C# spec §12.8.17.2.4 Collection initializers")]
    public Task CollectionInitializerAdd()
        => MakeMethodStaticCase.InDirectory("refuse/collection-initializer-add")
            .ExpectRefusal("implicit-reference")
            .NaiveShouldFail()
            .RunAsync();

    [Fact]
    [Spec("C# spec §6.5.5 Conditional compilation directives")]
    public Task Preprocessor()
        => MakeMethodStaticCase.InDirectory("refuse/preprocessor")
            .ExpectRefusal("preprocessor-region")
            .RunAsync();

    [Fact]
    [Spec("C# spec §15.6.3 Static and instance methods")]
    public Task AlreadyStatic()
        => MakeMethodStaticCase.InDirectory("refuse/already-static")
            .ExpectRefusal("not-instance-method")
            .RunAsync();
}
