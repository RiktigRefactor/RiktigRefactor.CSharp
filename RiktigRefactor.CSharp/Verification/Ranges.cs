using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RiktigRefactor.CSharp.Protocol;

namespace RiktigRefactor.CSharp.Verification;

/// <summary>Converts between Roslyn spans and protocol ranges, whose lines and columns are 1-based and count Unicode scalar values.</summary>
public static class Ranges
{
    public static FileRange FromSpan(SyntaxTree tree, TextSpan span)
        => FromSpan(tree.FilePath, tree.GetText(), span);

    public static FileRange FromSpan(string filePath, SourceText text, TextSpan span)
        => new(filePath, ToPosition(text, span.Start), ToPosition(text, span.End));

    public static FileRange? FromLocation(Location location)
        => location.SourceTree is { } tree ? FromSpan(tree, location.SourceSpan) : null;

    public static Position ToPosition(SourceText text, int offset)
    {
        var line = text.Lines.GetLineFromPosition(offset);
        var scalars = 0;
        for (var index = line.Start; index < offset; index++)
        {
            if (!char.IsLowSurrogate(text[index]))
            {
                scalars++;
            }
        }

        return new Position(line.LineNumber + 1, scalars + 1);
    }

    public static int ToOffset(SourceText text, Position position)
    {
        var line = text.Lines[position.Line - 1];
        var offset = line.Start;
        for (var scalars = 1; scalars < position.Column; scalars++)
        {
            if (offset >= line.EndIncludingLineBreak)
            {
                throw new ArgumentOutOfRangeException(nameof(position), $"Column {position.Column} is past the end of line {position.Line}.");
            }

            offset += char.IsHighSurrogate(text[offset]) ? 2 : 1;
        }

        return offset;
    }
}
