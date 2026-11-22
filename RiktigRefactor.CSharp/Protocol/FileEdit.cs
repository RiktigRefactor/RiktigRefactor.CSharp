namespace RiktigRefactor.CSharp.Protocol;

public record FileEdit(string File, Position Start, Position End, string NewText);
