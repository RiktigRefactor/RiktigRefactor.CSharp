using RiktigRefactor.CSharp.MoveToInstanceMethod;
using RiktigRefactor.CSharp.Verification;

namespace RiktigRefactor.CSharp.Tool;

public static class MoveToInstanceMethodCommand
{
    public static Task<int> RunAsync(MoveToInstanceMethodArguments arguments, TextWriter standardOutput, TextWriter standardError, CancellationToken cancellationToken)
        => RefactoringCommand.RunAsync(
            new CommonArguments(arguments.Workspace, arguments.File, arguments.Apply, arguments.Json),
            (document, text, token) => MoveToInstanceMethodRefactoring.MoveAsync(
                document.Project.Solution,
                document.Id,
                Ranges.ToOffset(text, RefactoringCommand.ParsePosition(arguments.At)),
                arguments.Receiver,
                token),
            standardOutput,
            standardError,
            cancellationToken);
}
