using RiktigRefactor.CSharp.ConvertToInstanceMethod;
using RiktigRefactor.CSharp.Verification;

namespace RiktigRefactor.CSharp.Tool;

public static class ConvertToInstanceMethodCommand
{
    public static Task<int> RunAsync(ConvertToInstanceMethodArguments arguments, TextWriter standardOutput, TextWriter standardError, CancellationToken cancellationToken)
        => RefactoringCommand.RunAsync(
            new CommonArguments(arguments.Workspace, arguments.File, arguments.Apply, arguments.Json),
            (document, text, token) => ConvertToInstanceMethodRefactoring.MoveAsync(
                document.Project.Solution,
                document.Id,
                Ranges.ToOffset(text, RefactoringCommand.ParsePosition(arguments.At)),
                arguments.Receiver,
                token),
            standardOutput,
            standardError,
            cancellationToken);
}
