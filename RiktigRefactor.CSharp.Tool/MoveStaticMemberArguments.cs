namespace RiktigRefactor.CSharp.Tool;

public record MoveStaticMemberArguments(string Workspace, string File, string At, string Target, bool Apply, bool Json);
