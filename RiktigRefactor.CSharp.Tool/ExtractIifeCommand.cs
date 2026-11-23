using Microsoft.CodeAnalysis.Text;
using RiktigRefactor.CSharp.ExtractIife;
using RiktigRefactor.CSharp.Verification;

namespace RiktigRefactor.CSharp.Tool;

public static class ExtractIifeCommand
{
    public static Task<int> RunAsync(ExtractIifeArguments arguments, TextWriter standardOutput, TextWriter standardError, CancellationToken cancellationToken)
        => RefactoringCommand.RunAsync(
            new CommonArguments(arguments.Workspace, arguments.File, arguments.Apply, arguments.Json),
            (document, text, token) => ExtractIifeRefactoring.ExtractAsync(document.Project.Solution, document.Id, SelectedSpan(text, arguments), token),
            standardOutput,
            standardError,
            cancellationToken);

    static TextSpan SelectedSpan(SourceText text, ExtractIifeArguments arguments)
    {
        var start = Ranges.ToOffset(text, RefactoringCommand.ParsePosition(arguments.From));
        var end = Ranges.ToOffset(text, RefactoringCommand.ParsePosition(arguments.To));
        if (end < start)
        {
            throw new ArgumentException($"`--to {arguments.To}` is before `--from {arguments.From}`.");
        }

        return TextSpan.FromBounds(start, end);
    }
}
