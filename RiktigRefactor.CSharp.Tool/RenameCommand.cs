using RiktigRefactor.CSharp.Rename;
using RiktigRefactor.CSharp.Verification;

namespace RiktigRefactor.CSharp.Tool;

public static class RenameCommand
{
    public const int ExitSuccess = RefactoringCommand.ExitSuccess;
    public const int ExitRefused = RefactoringCommand.ExitRefused;
    public const int ExitError = RefactoringCommand.ExitError;

    public static Task<int> RunAsync(RenameArguments arguments, TextWriter standardOutput, TextWriter standardError, CancellationToken cancellationToken)
        => RefactoringCommand.RunAsync(
            new CommonArguments(arguments.Workspace, arguments.File, arguments.Apply, arguments.Json),
            (document, text, token) => RenameRefactoring.RenameAsync(
                document.Project.Solution,
                document.Id,
                Ranges.ToOffset(text, RefactoringCommand.ParsePosition(arguments.At)),
                arguments.NewName,
                token),
            standardOutput,
            standardError,
            cancellationToken);
}
