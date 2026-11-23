using RiktigRefactor.CSharp.MakeMethodStatic;

namespace RiktigRefactor.CSharp.Tests.Infrastructure;

/// <summary>
/// A Make method static test whose input (and, unless a refusal is expected, expected output) live as files in a directory under <c>Cases/MakeMethodStatic</c>.
/// The input marks the method with the cursor <c>/*|*/</c>, on its declaration or a reference.
/// </summary>
/// <param name="CaseDirectory">The case directory relative to <c>Cases/MakeMethodStatic</c>.</param>
/// <param name="ReceiverName">The receiver parameter's name, or null for the default.</param>
/// <param name="RefusalCode">The expected refusal code, or null if the refactoring should succeed.</param>
/// <param name="NaiveMustFail">True for edge cases: the naive implementation must produce a program that fails.</param>
public record MakeMethodStaticCase(string CaseDirectory, string? ReceiverName = null, string? RefusalCode = null, bool NaiveMustFail = false)
{
    public static MakeMethodStaticCase InDirectory(string caseDirectory) => new(caseDirectory);

    public MakeMethodStaticCase WithReceiverName(string name) => this with { ReceiverName = name };

    public MakeMethodStaticCase ExpectRefusal(string code) => this with { RefusalCode = code };

    public MakeMethodStaticCase NaiveShouldFail() => this with { NaiveMustFail = true };

    public Task RunAsync()
        => CaseRunner.RunAsync(
            "MakeMethodStatic/" + CaseDirectory,
            null,
            RefusalCode,
            NaiveMustFail,
            (solution, documentId, selection) => MakeMethodStaticRefactoring.MakeStaticAsync(solution, documentId, selection.Start, ReceiverName, TestContext.Current.CancellationToken),
            (solution, documentId, selection) => NaiveMakeMethodStatic.MakeStaticAsync(solution, documentId, selection.Start, ReceiverName));
}
