using Microsoft.CodeAnalysis;
using RiktigRefactor.CSharp.Protocol;
using RiktigRefactor.CSharp.Rewriting;

namespace RiktigRefactor.CSharp.MoveToInstanceMethod;

/// <summary>
/// Move static method to instance method: turns a static method into an instance method on the type of one of its parameters, which becomes <c>this</c>.
/// </summary>
public static class MoveToInstanceMethodRefactoring
{
    /// <summary>Compiler errors that mean the method collides with a declaration in its new type, or that something can't be accessed from its new place.</summary>
    static readonly Dictionary<string, string> RefusalCodesForErrors = new(StringComparer.Ordinal)
    {
        ["CS0111"] = RefusalCodes.ConflictingDeclaration,
        ["CS0102"] = RefusalCodes.ConflictingDeclaration,
        ["CS0122"] = RefusalCodes.Inaccessible,
        ["CS0271"] = RefusalCodes.Inaccessible,
        ["CS0272"] = RefusalCodes.Inaccessible,
    };

    /// <param name="receiverName">The name of the parameter that becomes the receiver.</param>
    public static async Task<RefactoringResult> MoveAsync(
        Solution solution,
        DocumentId documentId,
        int position,
        string receiverName,
        CancellationToken cancellationToken = default)
    {
        var (plan, refusal) = await MoveToInstanceMethodPlanner.PlanAsync(solution, documentId, position, receiverName, checkPreconditions: true, cancellationToken).ConfigureAwait(false);
        if (refusal is not null)
        {
            return RefactoringResult.Refuse(refusal);
        }

        return await RewriteAndVerify.RunAsync(
            solution,
            plan!.Edits,
            (name, _) => plan.DeletedSpans.Any(deleted => deleted.Span.Contains(name.Span) && solution.GetDocumentId(name.SyntaxTree) == deleted.Document),
            RefusalCodesForErrors,
            cancellationToken).ConfigureAwait(false);
    }
}
