using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;

namespace RiktigRefactor.CSharp.Protocol;

public record RefactoringResult(
    RefactoringStatus Status,
    IReadOnlyList<FileEdit> Edits,
    IReadOnlyList<Warning> Warnings,
    IReadOnlyList<RefusalReason> Reasons)
{
    /// <summary>Gets the solution after the refactoring, or null when refused.</summary>
    [JsonIgnore]
    public Solution? NewSolution { get; init; }

    public static RefactoringResult Refuse(RefusalReason reason)
        => new(RefactoringStatus.Refused, [], [], [reason]);

    public static RefactoringResult Refuse(string code, string message, FileRange? location = null, string? suggestion = null)
        => Refuse(new RefusalReason(code, message, location, suggestion));
}
