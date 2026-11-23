using RiktigRefactor.CSharp.MoveStaticMember;
using RiktigRefactor.CSharp.Verification;

namespace RiktigRefactor.CSharp.Tool;

public static class MoveStaticMemberCommand
{
    public static Task<int> RunAsync(MoveStaticMemberArguments arguments, TextWriter standardOutput, TextWriter standardError, CancellationToken cancellationToken)
        => RefactoringCommand.RunAsync(
            new CommonArguments(arguments.Workspace, arguments.File, arguments.Apply, arguments.Json),
            (document, text, token) => MoveStaticMemberRefactoring.MoveAsync(
                document.Project.Solution,
                document.Id,
                Ranges.ToOffset(text, RefactoringCommand.ParsePosition(arguments.At)),
                arguments.Target,
                token),
            standardOutput,
            standardError,
            cancellationToken);
}
