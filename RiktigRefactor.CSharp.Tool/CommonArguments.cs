namespace RiktigRefactor.CSharp.Tool;

/// <summary>The options every refactoring command takes.</summary>
public record CommonArguments(string Workspace, string File, bool Apply, bool Json);
