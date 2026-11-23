using RiktigRefactor.CSharp.Tests.Infrastructure;

namespace RiktigRefactor.CSharp.Tests;

public class ExpressionExtractIifeCaseTests
{
    // The example from the specification: locals become parameters of the static lambda.
    [Fact]
    [Spec("C# spec §12.13.2 Multiplication operator")]
    [Spec("C# spec §12.22 Anonymous function expressions")]
    public Task Arithmetic()
        => ExtractIifeCase.InDirectory("expression/arithmetic").RunAsync();

    // The overflow check applies to code textually inside `checked(...)`, which the lambda body still is.
    [Fact]
    [Spec("C# spec §12.8.20 The checked and unchecked operators")]
    public Task CheckedContext()
        => ExtractIifeCase.InDirectory("expression/checked-context").RunAsync();

    // The trace shows the extracted operands still run between the ones before and after them.
    [Fact]
    [Spec("C# spec §12.4.2 Operator precedence and associativity")]
    [Spec("C# spec §12.6.2.3 Run-time evaluation of argument lists")]
    public Task EvaluationOrder()
        => ExtractIifeCase.InDirectory("expression/evaluation-order").RunAsync();

    // The lambda returns int, and the int-to-long conversion still happens after it.
    [Fact]
    [Spec("C# spec §10.2.3 Implicit numeric conversions")]
    public Task ImplicitNumericConversion()
        => ExtractIifeCase.InDirectory("expression/implicit-numeric-conversion").RunAsync();

    // A property, through implicit `this`, and a field, through explicit `this`, are both reached through the `@this` parameter.
    [Fact]
    [Spec("C# spec §12.8.14 This access")]
    [Spec("C# spec §12.8.4 Simple names")]
    public Task InstanceMembers()
        => ExtractIifeCase.InDirectory("expression/instance-members").RunAsync();

    [Fact]
    [Spec("C# spec §12.8.10 Invocation expressions")]
    public Task NoVariables()
        => ExtractIifeCase.InDirectory("expression/no-variables").RunAsync();

    // The lambda call replaces the right operand, so it still runs only when the left operand is true.
    [Fact]
    [Spec("C# spec §12.17.2 Boolean conditional logical operators")]
    public Task ShortCircuit()
        => ExtractIifeCase.InDirectory("expression/short-circuit").RunAsync();
}
