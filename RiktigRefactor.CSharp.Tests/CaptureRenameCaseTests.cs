using RiktigRefactor.CSharp.Tests.Infrastructure;

namespace RiktigRefactor.CSharp.Tests;

public class CaptureRenameCaseTests
{
    // Inside namespace Inner, its own type `Local` is found before the compilation unit's using alias.
    [Fact]
    [Spec("C# spec §14.6.2 Using alias directives")]
    [Spec("C# spec §7.7 Namespace and type names")]
    public Task AliasCapturedByNamespaceType()
        => RenameCase.InDirectory("capture/alias-captured-by-namespace-type")
            .RenameTo("Local")
            .ExpectRefusal("binding-would-change")
            .NaiveShouldFail()
            .RunAsync();

    // Anonymous types with different member names are different types, so they stop being equal.
    [Fact]
    [Spec("C# spec §12.8.17.3 Anonymous object creation expressions")]
    public Task AnonymousTypeEqualityWithInferredName()
        => RenameCase.InDirectory("capture/anonymous-type-equality-with-inferred-name")
            .RenameTo("Size")
            .NaiveShouldFail()
            .RunAsync();

    // The anonymous type is formatted through object, so no member access on it changes binding; only its inferred member name changes.
    [Fact]
    [Spec("C# spec §12.8.17.3 Anonymous object creation expressions")]
    public Task AnonymousTypeMemberNameInInterpolation()
        => RenameCase.InDirectory("capture/anonymous-type-member-name-in-interpolation")
            .RenameTo("Size")
            .NaiveShouldFail()
            .RunAsync();

    // The anonymous type's member name is inferred from the property and shows in its ToString.
    [Fact]
    [Spec("C# spec §12.8.17.3 Anonymous object creation expressions")]
    public Task AnonymousTypeMemberNameInferredFromProperty()
        => RenameCase.InDirectory("capture/anonymous-type-member-name-inferred-from-property")
            .RenameTo("Title")
            .NaiveShouldFail()
            .RunAsync();

    // `[Mark]` must become `[@Serial]`: a plain `[Serial]` would also match `SerialAttribute` by the attribute suffix rule.
    [Fact]
    [Spec("C# spec §23.3 Attribute specification")]
    public Task AttributeClassRenamedToOtherAttributeShortName()
        => RenameCase.InDirectory("capture/attribute-class-renamed-to-other-attribute-short-name")
            .RenameTo("Serial")
            .NaiveShouldFail()
            .RunAsync();

    // The renamed member hides Base.Weight, so the reference to Base.Weight must become `base.Weight`.
    [Fact]
    [Spec("C# spec §7.6.2.3 Hiding through inheritance")]
    [Spec("C# spec §12.5 Member lookup")]
    public Task BaseMemberHidden()
        => RenameCase.InDirectory("capture/base-member-hidden")
            .RenameTo("Weight")
            .NaiveShouldFail()
            .RunAsync();

    // A collection initializer calls Add by overload resolution without naming it, so a renamed method can capture the element additions.
    [Fact]
    [Spec("C# spec §12.8.17.2.3 Collection initializers")]
    public Task CollectionInitializerAdd()
        => RenameCase.InDirectory("capture/collection-initializer-add")
            .RenameTo("Add")
            .ExpectRefusal("binding-would-change")
            .NaiveShouldFail()
            .RunAsync();

    // After the rename the reference to the field would be captured by the local `total`; it must be qualified.
    [Fact]
    [Spec("C# spec §7.6 Scopes")]
    [Spec("C# spec §12.8.4 Simple names")]
    public Task FieldCapturedByLocal()
        => RenameCase.InDirectory("capture/field-captured-by-local")
            .RenameTo("total")
            .NaiveShouldFail()
            .RunAsync();

    // Renaming T to Item would make `new Item()` create a T instead; it compiles and silently changes the result.
    [Fact]
    [Spec("C# spec §7.6 Scopes")]
    [Spec("C# spec §15.2.3 Type parameters")]
    public Task GenericTypeParameterCapturesType()
        => RenameCase.InDirectory("capture/generic-type-parameter-captures-type")
            .RenameTo("Item")
            .NaiveShouldFail()
            .RunAsync();

    // A lambda parameter may shadow an outer local, so the rename compiles but the inner reference to the local would bind to the parameter.
    // A local cannot be qualified, so the tool must refuse.
    [Fact]
    [Spec("C# spec §7.6 Scopes")]
    [Spec("C# spec §12.22 Anonymous function expressions")]
    public Task LambdaParameterCapturesOuterLocal()
        => RenameCase.InDirectory("capture/lambda-parameter-captures-outer-local")
            .RenameTo("offset")
            .ExpectRefusal("binding-would-change")
            .NaiveShouldFail()
            .RunAsync();

    // The existing reference to the property `offset` would be captured by the renamed local; the trace shows whether the property getter still runs.
    [Fact]
    [Spec("C# spec §7.6 Scopes")]
    [Spec("C# spec §12.8.4 Simple names")]
    public Task LocalRenamedToFieldName()
        => RenameCase.InDirectory("capture/local-renamed-to-field-name")
            .RenameTo("offset")
            .NaiveShouldFail()
            .RunAsync();

    // The static field must be qualified with its type once a local of the same name shadows it.
    [Fact]
    [Spec("C# spec §12.8.4 Simple names")]
    public Task LocalRenamedToStaticFieldName()
        => RenameCase.InDirectory("capture/local-renamed-to-static-field-name")
            .RenameTo("F")
            .NaiveShouldFail()
            .RunAsync();

    // The existing reference to the field `a` would be captured by the renamed parameter.
    [Fact]
    [Spec("C# spec §7.6 Scopes")]
    [Spec("C# spec §12.8.4 Simple names")]
    public Task ParameterRenamedToFieldName()
        => RenameCase.InDirectory("capture/parameter-renamed-to-field-name")
            .RenameTo("a")
            .NaiveShouldFail()
            .RunAsync();

    // `value` is the implicit parameter of the setter.
    [Fact]
    [Spec("C# spec §15.7.3 Accessors")]
    public Task SetterValueParameter()
        => RenameCase.InDirectory("capture/setter-value-parameter")
            .RenameTo("value")
            .ExpectRefusal("conflicting-declaration")
            .NaiveShouldFail()
            .RunAsync();

    // The tuple element name is inferred from the local, so renaming the local renames the element and breaks t.count.
    [Fact]
    [Spec("C# spec §12.8.6 Tuple literals")]
    public Task TupleElementNameInferredFromLocal()
        => RenameCase.InDirectory("capture/tuple-element-name-inferred-from-local")
            .RenameTo("total")
            .ExpectRefusal("would-introduce-errors")
            .NaiveShouldFail()
            .RunAsync();

    // After the rename, `Bar.Make()` would bind to the field Bar and silently call a different method that compiles fine.
    [Fact]
    [Spec("C# spec §7.6 Scopes")]
    [Spec("C# spec §12.8.4 Simple names")]
    public Task TypeCapturedByField()
        => RenameCase.InDirectory("capture/type-captured-by-field")
            .RenameTo("Bar")
            .NaiveShouldFail()
            .RunAsync();
}
