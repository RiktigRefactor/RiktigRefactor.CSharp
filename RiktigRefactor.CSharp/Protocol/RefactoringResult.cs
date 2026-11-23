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

    /// <summary>Gets the absolute directory that every <c>file</c> in this result is relative to, or null while paths are still absolute.</summary>
    public string? WorkspaceRoot { get; init; }

    /// <summary>Returns a copy whose file paths are relative to <paramref name="workspaceRoot"/> and use <c>/</c> on every OS.</summary>
    public RefactoringResult WithWorkspaceRelativePaths(string workspaceRoot)
    {
        string Relative(string file) => Path.GetRelativePath(workspaceRoot, file).Replace('\\', '/');
        FileRange? RelativeRange(FileRange? range) => range is null ? null : range with { File = Relative(range.File) };

        return this with
        {
            WorkspaceRoot = workspaceRoot,
            Edits = [.. Edits.Select(edit => edit with { File = Relative(edit.File) })],
            Warnings = [.. Warnings.Select(warning => warning with { Location = RelativeRange(warning.Location) })],
            Reasons = [.. Reasons.Select(reason => reason with { Location = RelativeRange(reason.Location) })],
        };
    }

    public static RefactoringResult Refuse(RefusalReason reason)
        => new(RefactoringStatus.Refused, [], [], [reason]);

    public static RefactoringResult Refuse(string code, string message, FileRange? location = null, string? suggestion = null)
        => Refuse(new RefusalReason(code, message, location, suggestion));
}
