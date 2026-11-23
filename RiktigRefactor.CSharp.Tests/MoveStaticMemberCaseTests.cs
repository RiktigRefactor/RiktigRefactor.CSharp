using RiktigRefactor.CSharp.Tests.Infrastructure;

namespace RiktigRefactor.CSharp.Tests;

public class MoveStaticMemberCaseTests
{
    // References through the type, a subclass, a namespace-qualified name, a method group, and unqualified from inside the source all become `Pricing.Tax`.
    [Fact]
    [Spec("C# spec §12.8.7 Member access")]
    [Spec("C# spec §10.8 Method group conversions")]
    public Task MethodReferences()
        => MoveStaticMemberCase.InDirectory("basic/method-references")
            .MoveTo("Shop.Pricing")
            .RunAsync();

    // A call found through `using static Shop.Order;` would no longer find the member, so it is qualified.
    [Fact]
    [Spec("C# spec §14.5.5 Using static directives")]
    public Task UsingStatic()
        => MoveStaticMemberCase.InDirectory("basic/using-static")
            .MoveTo("Shop.Pricing")
            .RunAsync();

    // `Rate` and `Calls` in the body are found through `Order`, and `Pricing` has members with the same names, so they are qualified.
    [Fact]
    [Spec("C# spec §12.8.4 Simple names")]
    public Task BodyUsesSourceMembers()
        => MoveStaticMemberCase.InDirectory("basic/body-uses-source-members")
            .MoveTo("Shop.Pricing")
            .RunAsync();

    // The field moves with its documentation comment into a nested type, and is re-indented.
    [Fact]
    [Spec("C# spec §15.5.2 Static and instance fields")]
    public Task FieldAndNestedTarget()
        => MoveStaticMemberCase.InDirectory("basic/field-and-nested-target")
            .MoveTo("Shop.Outer.Defaults")
            .RunAsync();

    [Fact]
    [Spec("C# spec §15.4 Constants")]
    public Task Constant()
        => MoveStaticMemberCase.InDirectory("basic/constant")
            .MoveTo("Settings")
            .RunAsync();

    // `"hi".Shout()` still finds the extension method, since the target is a static class in the same namespace.
    [Fact]
    [Spec("C# spec §12.8.10.3 Extension method invocations")]
    public Task ExtensionMethod()
        => MoveStaticMemberCase.InDirectory("basic/extension-method")
            .MoveTo("Shop.TextHelpers")
            .RunAsync();

    // `Describe(1)` in the target binds to `Describe(long)`, and would bind to the moved `Describe(int)`.
    [Fact]
    [Spec("C# spec §12.6.4 Overload resolution")]
    public Task OverloadCaptured()
        => MoveStaticMemberCase.InDirectory("refuse/overload-captured")
            .MoveTo("Pricing")
            .ExpectRefusal("binding-would-change")
            .NaiveShouldFail()
            .RunAsync();

    [Fact]
    [Spec("C# spec §7.7.2.3 Hiding through inheritance")]
    public Task HidesBaseMember()
        => MoveStaticMemberCase.InDirectory("refuse/hides-base-member")
            .MoveTo("Pricing")
            .ExpectRefusal("name-collision")
            .NaiveShouldFail()
            .RunAsync();

    [Fact]
    [Spec("C# spec §15.3.10 Member names reserved")]
    public Task NameCollision()
        => MoveStaticMemberCase.InDirectory("refuse/name-collision")
            .MoveTo("Pricing")
            .ExpectRefusal("name-collision")
            .NaiveShouldFail()
            .RunAsync();

    // Calling `Order.One()` runs `Order`'s static constructor; `Pricing.One()` would not.
    [Fact]
    [Spec("C# spec §15.12 Static constructors")]
    public Task SourceStaticConstructor()
        => MoveStaticMemberCase.InDirectory("refuse/source-static-constructor")
            .MoveTo("Pricing")
            .ExpectRefusal("initialization-order")
            .NaiveShouldFail()
            .RunAsync();

    [Fact]
    [Spec("C# spec §15.12 Static constructors")]
    public Task TargetStaticConstructor()
        => MoveStaticMemberCase.InDirectory("refuse/target-static-constructor")
            .MoveTo("Pricing")
            .ExpectRefusal("initialization-order")
            .NaiveShouldFail()
            .RunAsync();

    [Fact]
    [Spec("C# spec §15.5.6.2 Static field initialization")]
    public Task NonConstantInitializer()
        => MoveStaticMemberCase.InDirectory("refuse/non-constant-initializer")
            .MoveTo("Pricing")
            .ExpectRefusal("initialization-order")
            .RunAsync();

    // Each constructed `Box<T>` has its own `Count`; moving it would share one field.
    [Fact]
    [Spec("C# spec §15.5.2 Static and instance fields")]
    public Task GenericSource()
        => MoveStaticMemberCase.InDirectory("refuse/generic-source")
            .MoveTo("Counters")
            .ExpectRefusal("generic-type")
            .NaiveShouldFail()
            .RunAsync();

    [Fact]
    [Spec("C# spec §13.13 The lock statement")]
    public Task LockOnType()
        => MoveStaticMemberCase.InDirectory("refuse/lock-on-type")
            .MoveTo("Pricing")
            .ExpectRefusal("lock-on-type")
            .RunAsync();

    [Fact]
    [Spec("C# spec §7.5.3 Accessibility domains")]
    public Task Inaccessible()
        => MoveStaticMemberCase.InDirectory("refuse/inaccessible")
            .MoveTo("Pricing")
            .ExpectRefusal("inaccessible")
            .NaiveShouldFail()
            .RunAsync();

    [Fact]
    [Spec("C# spec §15.6.3 Static and instance methods")]
    public Task InstanceMember()
        => MoveStaticMemberCase.InDirectory("refuse/instance-member")
            .MoveTo("Pricing")
            .ExpectRefusal("not-movable")
            .RunAsync();

    [Fact]
    [Spec("C# spec §7.6.1 General")]
    public Task UnknownTarget()
        => MoveStaticMemberCase.InDirectory("refuse/unknown-target")
            .MoveTo("Nowhere")
            .ExpectRefusal("invalid-target")
            .RunAsync();

    [Fact]
    [Spec("C# spec §6.5.5 Conditional compilation directives")]
    public Task Preprocessor()
        => MoveStaticMemberCase.InDirectory("refuse/preprocessor")
            .MoveTo("Pricing")
            .ExpectRefusal("preprocessor-region")
            .RunAsync();
}
