using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RiktigRefactor.CSharp.Protocol;
using RiktigRefactor.CSharp.Verification;

namespace RiktigRefactor.CSharp.ExtractIife;

/// <summary>
/// Extract IIFE (also called extract static lambda): wraps a selected expression or statements in an immediately-invoked static lambda.
/// Variables from outside are passed in as parameters, and those the selection assigns and the code after it reads are returned.
/// </summary>
public static class ExtractIifeRefactoring
{
    public static async Task<RefactoringResult> ExtractAsync(
        Solution solution,
        DocumentId documentId,
        TextSpan selectedSpan,
        CancellationToken cancellationToken = default)
    {
        var document = solution.GetDocument(documentId) ?? throw new ArgumentException("Document not in solution.", nameof(documentId));
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var selectionLocation = Ranges.FromSpan(document.FilePath ?? document.Name, text, selectedSpan);

        var selection = IifeSelection.Find(root!, selectedSpan);
        if (selection is null)
        {
            return RefactoringResult.Refuse(
                RefusalCodes.InvalidSelection,
                "The selection is not exactly one expression, or whole statements in one block.",
                selectionLocation,
                "Select a complete expression, or complete statements.");
        }

        var plan = IifePlanner.Plan(model!, selection, cancellationToken);
        if (plan is null)
        {
            return RefactoringResult.Refuse(RefusalCodes.InvalidSelection, "Roslyn could not analyze the data flow of the selection.", selectionLocation);
        }

        if (IifePreconditions.FindProblem(model!, plan, cancellationToken) is { } problem)
        {
            return RefactoringResult.Refuse(problem);
        }

        var iife = IifeWriter.Write(model!, text, plan);
        if (iife is null)
        {
            return RefactoringResult.Refuse(RefusalCodes.TooManyVariables, "The lambda would need more parameters than `Func` and `Action` allow.", selectionLocation);
        }

        var newDocument = document.WithText(text.WithChanges(new TextChange(iife.ReplacedSpan, iife.NewText)));
        var newSolution = newDocument.Project.Solution;

        var newErrors = await NewErrorsAsync(solution, newSolution, cancellationToken).ConfigureAwait(false);
        if (newErrors.Count > 0)
        {
            return RefactoringResult.Refuse(
                RefusalCodes.WouldIntroduceErrors,
                $"The extracted code would not compile: {string.Join("; ", newErrors)}",
                selectionLocation);
        }

        var verificationProblem = await IifeVerifier.FindProblemAsync(document, newDocument, plan, iife, cancellationToken).ConfigureAwait(false);
        if (verificationProblem is not null)
        {
            return RefactoringResult.Refuse(RefusalCodes.InternalVerificationFailed, verificationProblem, selectionLocation, "Please report this as a bug.");
        }

        var edit = new FileEdit(
            document.FilePath ?? document.Name,
            Ranges.ToPosition(text, iife.ReplacedSpan.Start),
            Ranges.ToPosition(text, iife.ReplacedSpan.End),
            iife.NewText);
        return new RefactoringResult(RefactoringStatus.Success, [edit], [], []) { NewSolution = newSolution };
    }

    static async Task<List<string>> NewErrorsAsync(Solution oldSolution, Solution newSolution, CancellationToken cancellationToken)
    {
        var before = await ErrorCountsAsync(oldSolution, cancellationToken).ConfigureAwait(false);
        var after = await ErrorCountsAsync(newSolution, cancellationToken).ConfigureAwait(false);
        return [.. after.Where(pair => pair.Value.Count > (before.TryGetValue(pair.Key, out var old) ? old.Count : 0)).Select(pair => pair.Value.First)];
    }

    static async Task<Dictionary<(string File, string Id), (int Count, string First)>> ErrorCountsAsync(Solution solution, CancellationToken cancellationToken)
    {
        var counts = new Dictionary<(string File, string Id), (int Count, string First)>();
        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            foreach (var error in compilation?.GetDiagnostics(cancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error) ?? [])
            {
                var key = (error.Location.SourceTree?.FilePath ?? project.Name, error.Id);
                var existing = counts.TryGetValue(key, out var found) ? found : (Count: 0, First: error.ToString());
                counts[key] = (existing.Count + 1, existing.First);
            }
        }

        return counts;
    }
}
