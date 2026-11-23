using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RiktigRefactor.CSharp.Protocol;
using RiktigRefactor.CSharp.Rewriting;
using RiktigRefactor.CSharp.Verification;

namespace RiktigRefactor.CSharp.MakeMethodStatic;

/// <summary>
/// Make method static: turns an instance method into a static method on the same type, with the receiver as a new first parameter, and rewrites every call.
/// </summary>
public static class MakeMethodStaticRefactoring
{
    /// <summary>Compiler errors that mean the new signature collides with an existing declaration.</summary>
    static readonly HashSet<string> DeclarationConflictErrors = ["CS0100", "CS0111", "CS0128", "CS0136", "CS0412"];

    /// <param name="receiverName">The name of the new receiver parameter, or null for the type's name in camel case.</param>
    public static async Task<RefactoringResult> MakeStaticAsync(
        Solution solution,
        DocumentId documentId,
        int position,
        string? receiverName = null,
        CancellationToken cancellationToken = default)
    {
        var (plan, refusal) = await MakeMethodStaticPlanner.PlanAsync(solution, documentId, position, receiverName, checkPreconditions: true, cancellationToken).ConfigureAwait(false);
        if (refusal is not null)
        {
            return RefactoringResult.Refuse(refusal);
        }

        return await RewriteAndVerifyAsync(solution, plan!.Edits, (name, model) => IsCallTo(plan.Method, name, model), DeclarationConflictErrors, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Applies the edits, then refuses if they add a compile error or change what any name binds to.</summary>
    public static async Task<RefactoringResult> RewriteAndVerifyAsync(
        Solution solution,
        IReadOnlyList<SourceEdit> edits,
        Func<SimpleNameSyntax, SemanticModel, bool> mayDisappear,
        IReadOnlySet<string> declarationConflictErrors,
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
            var isConflict = newErrors.Any(error => declarationConflictErrors.Contains(error.Id));
            return RefactoringResult.Refuse(
                isConflict ? RefusalCodes.ConflictingDeclaration : RefusalCodes.WouldIntroduceErrors,
                $"The change would not compile: {string.Join("; ", newErrors.Select(error => $"{error.Id}: {error.GetMessage(System.Globalization.CultureInfo.InvariantCulture)}"))}",
                null);
        }

        var problem = await RewriteVerifier.FindProblemAsync(rewrite, mayDisappear, cancellationToken).ConfigureAwait(false);
        if (problem is not null)
        {
            return RefactoringResult.Refuse(problem);
        }

        return new RefactoringResult(RefactoringStatus.Success, rewrite.FileEdits, [], []) { NewSolution = rewrite.NewSolution };
    }

    static bool IsCallTo(IMethodSymbol method, SimpleNameSyntax name, SemanticModel model)
        => model.GetSymbolInfo(name).Symbol is IMethodSymbol called && SymbolEqualityComparer.Default.Equals(SymbolIdentity.Normalize(called), method);
}
