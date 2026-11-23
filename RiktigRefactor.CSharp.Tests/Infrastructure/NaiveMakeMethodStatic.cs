using Microsoft.CodeAnalysis;
using RiktigRefactor.CSharp.MakeMethodStatic;
using RiktigRefactor.CSharp.Rewriting;

namespace RiktigRefactor.CSharp.Tests.Infrastructure;

/// <summary>
/// Rewrites the method and its calls the same way the real refactoring does, but with no preconditions and no verification.
/// Edge-case tests must fail under this implementation, which proves they can detect a behavior change.
/// </summary>
public static class NaiveMakeMethodStatic
{
    public static async Task<Solution> MakeStaticAsync(Solution solution, DocumentId documentId, int position, string? receiverName)
    {
        var (plan, refusal) = await MakeMethodStaticPlanner.PlanAsync(solution, documentId, position, receiverName, checkPreconditions: false, CancellationToken.None);
        if (plan is null)
        {
            throw new InvalidOperationException($"The naive implementation can't plan this case: {refusal}");
        }

        return (await RewrittenSolution.ApplyAsync(solution, plan.Edits, CancellationToken.None)).NewSolution;
    }
}
