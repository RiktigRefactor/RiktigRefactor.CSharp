using RiktigRefactor.CSharp.Tests.Infrastructure;

namespace RiktigRefactor.CSharp.Tests;

public class StatementsExtractIifeCaseTests
{
    // Variables the selection declares and the code after it uses are declared at the call site.
    [Fact]
    [Spec("C# spec §12.24.3 Deconstructing assignment")]
    [Spec("C# spec §7.6 Scopes")]
    public Task DeclaredVariableUsedAfter()
        => ExtractIifeCase.InDirectory("statements/declared-variable-used-after").RunAsync();

    // The instance method calls go through the `@this` parameter, so they change the original object.
    [Fact]
    [Spec("C# spec §12.8.14 This access")]
    public Task InstanceMembers()
        => ExtractIifeCase.InDirectory("statements/instance-members").RunAsync();

    [Fact]
    [Spec("C# spec §6.4.3 Identifiers")]
    public Task KeywordNamedVariable()
        => ExtractIifeCase.InDirectory("statements/keyword-named-variable").RunAsync();

    // The value written in one iteration is read by the next, so it must be returned.
    [Fact]
    [Spec("C# spec §9.4 Definite assignment")]
    [Spec("C# spec §12.24.5 Compound assignment")]
    public Task LoopCarriedWrite()
        => ExtractIifeCase.InDirectory("statements/loop-carried-write").RunAsync();

    // An assigned outer variable and a newly declared one are returned together and deconstructed.
    [Fact]
    [Spec("C# spec §12.24.3 Deconstructing assignment")]
    public Task MixedAssignedAndDeclared()
        => ExtractIifeCase.InDirectory("statements/mixed-assigned-and-declared").RunAsync();

    // Indenting the second line of a verbatim string would change the string, so the body keeps its indentation.
    [Fact]
    [Spec("C# spec §6.4.5.6 String literals")]
    public Task MultiLineVerbatimString()
        => ExtractIifeCase.InDirectory("statements/multi-line-verbatim-string").RunAsync();

    // The nested lambda captures only `scale`, which is declared inside the selection, so the capture moves with it.
    [Fact]
    [Spec("C# spec §12.22.6.2 Captured outer variables")]
    public Task NestedLambdaDeclaredInside()
        => ExtractIifeCase.InDirectory("statements/nested-lambda-declared-inside").RunAsync();

    [Fact]
    [Spec("C# spec §12.22 Anonymous function expressions")]
    public Task NoOutputs()
        => ExtractIifeCase.InDirectory("statements/no-outputs").RunAsync();

    // Roslyn's data flow counts calling a mutating method on a struct local as a write, so the lambda returns the changed struct.
    [Fact]
    [Spec("C# spec §16.8 Class and struct differences")]
    public Task StructLocalMutatedByMethod()
        => ExtractIifeCase.InDirectory("statements/struct-local-mutated-by-method").RunAsync();

    // `result` is not definitely assigned before the selection, so it can't be passed in; the lambda declares its own and returns it.
    [Fact]
    [Spec("C# spec §9.4 Definite assignment")]
    public Task UnassignedVariableAssignedInside()
        => ExtractIifeCase.InDirectory("statements/unassigned-variable-assigned-inside").RunAsync();

    // The spec's example: assigned variables that are read afterwards come back in a tuple.
    [Fact]
    [Spec("C# spec §12.24.3 Deconstructing assignment")]
    [Spec("C# spec §12.8.16 Postfix increment and decrement operators")]
    public Task WritesReturnedInTuple()
        => ExtractIifeCase.InDirectory("statements/writes-returned-in-tuple").RunAsync();

    // `scratch` is written and used only inside, so the lambda declares its own and returns nothing.
    [Fact]
    [Spec("C# spec §9.4 Definite assignment")]
    public Task WriteNotReadAfter()
        => ExtractIifeCase.InDirectory("statements/write-not-read-after").RunAsync();
}
