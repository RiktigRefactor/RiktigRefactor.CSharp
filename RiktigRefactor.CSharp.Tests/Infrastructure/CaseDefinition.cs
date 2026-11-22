namespace RiktigRefactor.CSharp.Tests.Infrastructure;

/// <param name="Refuse">The expected refusal code, or null if the refactoring should succeed.</param>
/// <param name="NaiveShouldFail">True for edge cases: the naive implementation must produce a program that fails.</param>
/// <param name="Spec">C# specification sections this case covers.</param>
public record CaseDefinition(
    IReadOnlyDictionary<string, string>? Options,
    string? Refuse,
    bool NaiveShouldFail,
    IReadOnlyList<string>? Spec,
    string? Notes);
