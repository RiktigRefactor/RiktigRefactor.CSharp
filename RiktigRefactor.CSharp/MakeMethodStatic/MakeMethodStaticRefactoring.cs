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
    static readonly Dictionary<string, string> RefusalCodesForErrors = new[] { "CS0100", "CS0111", "CS0128", "CS0136", "CS0412" }
        .ToDictionary(id => id, _ => RefusalCodes.ConflictingDeclaration, StringComparer.Ordinal);

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

        return await RewriteAndVerify.RunAsync(solution, plan!.Edits, (name, model) => IsCallTo(plan.Method, name, model), RefusalCodesForErrors, cancellationToken).ConfigureAwait(false);
    }

    static bool IsCallTo(IMethodSymbol method, SimpleNameSyntax name, SemanticModel model)
        => model.GetSymbolInfo(name).Symbol is IMethodSymbol called && SymbolEqualityComparer.Default.Equals(SymbolIdentity.Normalize(called), method);
}
