using RiktigRefactor.CSharp.MakeMethodStatic;
using RiktigRefactor.CSharp.Verification;

namespace RiktigRefactor.CSharp.Tool;

public static class MakeMethodStaticCommand
{
    public static Task<int> RunAsync(MakeMethodStaticArguments arguments, TextWriter standardOutput, TextWriter standardError, CancellationToken cancellationToken)
        => RefactoringCommand.RunAsync(
            new CommonArguments(arguments.Workspace, arguments.File, arguments.Apply, arguments.Json),
            (document, text, token) => MakeMethodStaticRefactoring.MakeStaticAsync(
                document.Project.Solution,
                document.Id,
                Ranges.ToOffset(text, RefactoringCommand.ParsePosition(arguments.At)),
                arguments.ReceiverName,
                token),
            standardOutput,
            standardError,
            cancellationToken);
}
