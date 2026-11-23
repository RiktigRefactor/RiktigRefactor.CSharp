using RiktigRefactor.CSharp.MoveStaticMember;

namespace RiktigRefactor.CSharp.Tests.Infrastructure;

/// <summary>
/// A Move static member test whose input (and, unless a refusal is expected, expected output) live as files in a directory under <c>Cases/MoveStaticMember</c>.
/// The input marks the member with the cursor <c>/*|*/</c>, on its declaration or a reference.
/// </summary>
/// <param name="CaseDirectory">The case directory relative to <c>Cases/MoveStaticMember</c>.</param>
/// <param name="Target">The destination type's qualified name.</param>
/// <param name="RefusalCode">The expected refusal code, or null if the refactoring should succeed.</param>
/// <param name="NaiveMustFail">True for edge cases: the naive implementation must produce a program that fails.</param>
public record MoveStaticMemberCase(string CaseDirectory, string? Target = null, string? RefusalCode = null, bool NaiveMustFail = false)
{
    public static MoveStaticMemberCase InDirectory(string caseDirectory) => new(caseDirectory);

    public MoveStaticMemberCase MoveTo(string target) => this with { Target = target };

    public MoveStaticMemberCase ExpectRefusal(string code) => this with { RefusalCode = code };

    public MoveStaticMemberCase NaiveShouldFail() => this with { NaiveMustFail = true };

    public Task RunAsync()
    {
        var target = Target ?? throw new InvalidOperationException($"{CaseDirectory}: call MoveTo.");
        return CaseRunner.RunAsync(
            "MoveStaticMember/" + CaseDirectory,
            null,
            RefusalCode,
            NaiveMustFail,
            (solution, documentId, selection) => MoveStaticMemberRefactoring.MoveAsync(solution, documentId, selection.Start, target, TestContext.Current.CancellationToken),
            (solution, documentId, selection) => NaiveMoveStaticMember.MoveAsync(solution, documentId, selection.Start, target));
    }
}
