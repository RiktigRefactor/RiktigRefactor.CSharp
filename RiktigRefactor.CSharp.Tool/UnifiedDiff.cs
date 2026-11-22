using DiffPlex;
using DiffPlex.Renderer;

namespace RiktigRefactor.CSharp.Tool;

/// <summary>Formats the difference between two versions of a file as a unified diff.</summary>
public static class UnifiedDiff
{
    const int ContextLines = 3;

    static readonly UnidiffRenderer Renderer = new(new Differ(), ContextLines);

    public static string Format(string displayPath, string oldText, string newText)
    {
        var oldTextWithoutTrailingNewline = NormalizeLineEndingsAndRemoveTrailingNewline(oldText);
        var newTextWithoutTrailingNewline = NormalizeLineEndingsAndRemoveTrailingNewline(newText);
        if (oldTextWithoutTrailingNewline == newTextWithoutTrailingNewline)
        {
            return string.Empty;
        }

        var (oldHeader, newHeader) = Path.IsPathRooted(displayPath) ? (displayPath, displayPath) : ($"a/{displayPath}", $"b/{displayPath}");
        return Renderer.Generate(oldTextWithoutTrailingNewline, newTextWithoutTrailingNewline, oldHeader, newHeader);
    }

    /// <remarks>The renderer counts the empty text after a final newline as an extra line, which would make hunk headers wrong.</remarks>
    static string NormalizeLineEndingsAndRemoveTrailingNewline(string text)
    {
        return text.Replace("\r\n", "\n").TrimEnd('\n');
    }
}
