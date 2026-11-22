namespace RiktigRefactor.CSharp.Protocol;

public record RefusalReason(string Code, string Message, FileRange? Location, string? Suggestion);
