namespace RiktigRefactor.CSharp.Protocol;

public record Warning(string Code, string Message, FileRange? Location);
