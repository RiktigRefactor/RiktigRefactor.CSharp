namespace RiktigRefactor.CSharp.Tests.Infrastructure;

/// <param name="Refuse">The expected refusal code, or null if the refactoring should succeed.</param>
/// <param name="NaiveShouldFail">True for edge cases: the naive implementation must produce a program that fails.</param>
/// <param name="Projects">For multi-project cases: project name to the names of projects it references, in build order. The last project is the program entry point. A file belongs to the project named by its first path segment. Null means a single project.</param>
/// <param name="Spec">C# specification sections this case covers.</param>
public record CaseDefinition(
    IReadOnlyDictionary<string, string>? Options,
    string? Refuse,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? Projects,
    bool NaiveShouldFail,
    IReadOnlyList<string>? Spec,
    string? Notes);
