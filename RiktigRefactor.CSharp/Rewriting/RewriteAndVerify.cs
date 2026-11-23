using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RiktigRefactor.CSharp.Protocol;
using RiktigRefactor.CSharp.Verification;

namespace RiktigRefactor.CSharp.Rewriting;

/// <summary>The last steps every rewriting refactoring shares: apply the edits, then refuse if they add a compile error or change what any name binds to.</summary>
public static class RewriteAndVerify
{
    /// <param name="mayDisappear">True for an original name that the refactoring deletes on purpose.</param>
    /// <param name="refusalCodesForErrors">Refusal codes for compiler errors that have a more specific meaning than <c>would-introduce-errors</c>, by error ID.</param>
    public static async Task<RefactoringResult> RunAsync(
        Solution solution,
        IReadOnlyList<SourceEdit> edits,
        Func<SimpleNameSyntax, SemanticModel, bool> mayDisappear,
        IReadOnlyDictionary<string, string> refusalCodesForErrors,
        CancellationToken cancellationToken)
    {
        RewrittenSolution rewrite;
        try
        {
            rewrite = await RewrittenSolution.ApplyAsync(solution, edits, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException exception)
        {
            return RefactoringResult.Refuse(RefusalCodes.InternalVerificationFailed, exception.Message, null, "Please report this as a bug.");
        }

        var newErrors = await CompileErrors.FindNewAsync(solution, rewrite.NewSolution, cancellationToken).ConfigureAwait(false);
        if (newErrors.Count > 0)
        {
            var code = newErrors.Select(error => refusalCodesForErrors.GetValueOrDefault(error.Id)).FirstOrDefault(found => found is not null);
            return RefactoringResult.Refuse(
                code ?? RefusalCodes.WouldIntroduceErrors,
                $"The change would not compile: {string.Join("; ", newErrors.Select(error => $"{error.Id}: {error.GetMessage(System.Globalization.CultureInfo.InvariantCulture)}"))}",
                Ranges.FromLocation(newErrors[0].Location));
        }

        var problem = await RewriteVerifier.FindProblemAsync(rewrite, mayDisappear, cancellationToken).ConfigureAwait(false);
        if (problem is not null)
        {
            return RefactoringResult.Refuse(problem);
        }

        return new RefactoringResult(RefactoringStatus.Success, rewrite.FileEdits, [], []) { NewSolution = rewrite.NewSolution };
    }
}
