using Microsoft.CodeAnalysis;
using RiktigRefactor.CSharp.MoveStaticMember;
using RiktigRefactor.CSharp.Rewriting;

namespace RiktigRefactor.CSharp.Tests.Infrastructure;

/// <summary>
/// Moves the member and rewrites its references the same way the real refactoring does, but with no preconditions and no verification.
/// Edge-case tests must fail under this implementation, which proves they can detect a behavior change.
/// </summary>
public static class NaiveMoveStaticMember
{
    public static async Task<Solution> MoveAsync(Solution solution, DocumentId documentId, int position, string target)
    {
        var (plan, refusal) = await MoveStaticMemberPlanner.PlanAsync(solution, documentId, position, target, checkPreconditions: false, CancellationToken.None);
        if (plan is null)
        {
            throw new InvalidOperationException($"The naive implementation can't plan this case: {refusal}");
        }

        return (await RewrittenSolution.ApplyAsync(solution, plan.Edits, CancellationToken.None)).NewSolution;
    }
}
