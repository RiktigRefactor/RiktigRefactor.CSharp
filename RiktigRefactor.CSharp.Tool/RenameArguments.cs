namespace RiktigRefactor.CSharp.Tool;

public record RenameArguments(string Workspace, string File, string At, string NewName, bool Apply, bool Json);
