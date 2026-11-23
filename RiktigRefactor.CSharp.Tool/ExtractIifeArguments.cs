namespace RiktigRefactor.CSharp.Tool;

public record ExtractIifeArguments(string Workspace, string File, string From, string To, bool Apply, bool Json);
