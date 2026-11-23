using RiktigRefactor.CSharp.MoveToInstanceMethod;

namespace RiktigRefactor.CSharp.Tests.Infrastructure;

/// <summary>
/// A Move static method to instance method test whose input (and, unless a refusal is expected, expected output) live as files in a directory under <c>Cases/MoveToInstanceMethod</c>.
/// The input marks the method with the cursor <c>/*|*/</c>, on its declaration or a reference.
/// </summary>
/// <param name="CaseDirectory">The case directory relative to <c>Cases/MoveToInstanceMethod</c>.</param>
/// <param name="Receiver">The name of the parameter that becomes the receiver.</param>
/// <param name="RefusalCode">The expected refusal code, or null if the refactoring should succeed.</param>
/// <param name="NaiveMustFail">True for edge cases: the naive implementation must produce a program that fails.</param>
public record MoveToInstanceMethodCase(string CaseDirectory, string? Receiver = null, string? RefusalCode = null, bool NaiveMustFail = false)
{
    public static MoveToInstanceMethodCase InDirectory(string caseDirectory) => new(caseDirectory);

    public MoveToInstanceMethodCase WithReceiver(string receiver) => this with { Receiver = receiver };

    public MoveToInstanceMethodCase ExpectRefusal(string code) => this with { RefusalCode = code };

    public MoveToInstanceMethodCase NaiveShouldFail() => this with { NaiveMustFail = true };

    public Task RunAsync()
    {
        var receiver = Receiver ?? throw new InvalidOperationException($"{CaseDirectory}: call WithReceiver.");
        return CaseRunner.RunAsync(
            "MoveToInstanceMethod/" + CaseDirectory,
            null,
            RefusalCode,
            NaiveMustFail,
            (solution, documentId, selection) => MoveToInstanceMethodRefactoring.MoveAsync(solution, documentId, selection.Start, receiver, TestContext.Current.CancellationToken),
            (solution, documentId, selection) => NaiveMoveToInstanceMethod.MoveAsync(solution, documentId, selection.Start, receiver));
    }
}
