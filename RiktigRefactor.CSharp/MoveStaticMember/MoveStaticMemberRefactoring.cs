using Microsoft.CodeAnalysis;
using RiktigRefactor.CSharp.Protocol;
using RiktigRefactor.CSharp.Rewriting;

namespace RiktigRefactor.CSharp.MoveStaticMember;

/// <summary>
/// Move static member: moves a static method, field, property, event or constant to another type, and updates every reference.
/// </summary>
public static class MoveStaticMemberRefactoring
{
    /// <summary>Compiler errors that mean the moved member collides with a declaration in the target, or that something can't be accessed from its new place.</summary>
    static readonly Dictionary<string, string> RefusalCodesForErrors = new(StringComparer.Ordinal)
    {
        ["CS0102"] = RefusalCodes.ConflictingDeclaration,
        ["CS0111"] = RefusalCodes.ConflictingDeclaration,
        ["CS0542"] = RefusalCodes.ConflictingDeclaration,
        ["CS0122"] = RefusalCodes.Inaccessible,
        ["CS0271"] = RefusalCodes.Inaccessible,
        ["CS0272"] = RefusalCodes.Inaccessible,
        ["CS1540"] = RefusalCodes.Inaccessible,
    };

    /// <param name="target">The destination type's namespace-qualified name, such as <c>Shop.Pricing</c>.</param>
    public static async Task<RefactoringResult> MoveAsync(
        Solution solution,
        DocumentId documentId,
        int position,
        string target,
        CancellationToken cancellationToken = default)
    {
        var (plan, refusal) = await MoveStaticMemberPlanner.PlanAsync(solution, documentId, position, target, checkPreconditions: true, cancellationToken).ConfigureAwait(false);
        if (refusal is not null)
        {
            return RefactoringResult.Refuse(refusal);
        }

        return await RewriteAndVerify.RunAsync(
            solution,
            plan!.Edits,
            (name, _) => plan.ReplacedQualifiers.Any(replaced => replaced.Span.Contains(name.Span) && solution.GetDocumentId(name.SyntaxTree) == replaced.Document),
            RefusalCodesForErrors,
            cancellationToken).ConfigureAwait(false);
    }
}
