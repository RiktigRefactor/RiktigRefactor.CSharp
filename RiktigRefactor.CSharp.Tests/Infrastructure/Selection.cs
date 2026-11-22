namespace RiktigRefactor.CSharp.Tests.Infrastructure;

/// <summary>A cursor (when <see cref="Start"/> equals <see cref="End"/>) or range in one file, as offsets into the marker-free text.</summary>
public record Selection(string File, int Start, int End);
