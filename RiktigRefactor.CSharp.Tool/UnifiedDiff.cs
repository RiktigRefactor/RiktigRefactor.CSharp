using System.Text;

namespace RiktigRefactor.CSharp.Tool;

/// <summary>Formats the difference between two versions of a file as a unified diff.</summary>
public static class UnifiedDiff
{
    const int ContextLines = 3;

    public static string Format(string displayPath, string oldText, string newText)
    {
        var oldLines = SplitLines(oldText);
        var newLines = SplitLines(newText);
        var changes = FindChanges(oldLines, newLines);
        if (changes.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        var (oldHeader, newHeader) = Path.IsPathRooted(displayPath) ? (displayPath, displayPath) : ($"a/{displayPath}", $"b/{displayPath}");
        builder.Append("--- ").Append(oldHeader).Append('\n');
        builder.Append("+++ ").Append(newHeader).Append('\n');
        foreach (var hunk in GroupIntoHunks(changes))
        {
            AppendHunk(builder, oldLines, newLines, hunk);
        }

        return builder.ToString();
    }

    static List<string> SplitLines(string text)
    {
        var lines = text.Split('\n').Select(line => line.TrimEnd('\r')).ToList();
        if (lines[^1].Length == 0)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        return lines;
    }

    /// <summary>
    /// Finds the changed line ranges. A rename keeps the line count, so lines are compared one by one;
    /// otherwise the whole region between the common prefix and suffix is treated as one change.
    /// </summary>
    static List<Change> FindChanges(List<string> oldLines, List<string> newLines)
    {
        var changes = new List<Change>();
        if (oldLines.Count == newLines.Count)
        {
            for (var index = 0; index < oldLines.Count; index++)
            {
                if (oldLines[index] != newLines[index])
                {
                    changes.Add(new Change(index, 1, index, 1));
                }
            }

            return changes;
        }

        var prefix = 0;
        while (prefix < oldLines.Count && prefix < newLines.Count && oldLines[prefix] == newLines[prefix])
        {
            prefix++;
        }

        var suffix = 0;
        while (suffix < oldLines.Count - prefix && suffix < newLines.Count - prefix && oldLines[^(suffix + 1)] == newLines[^(suffix + 1)])
        {
            suffix++;
        }

        changes.Add(new Change(prefix, oldLines.Count - prefix - suffix, prefix, newLines.Count - prefix - suffix));
        return changes;
    }

    static List<List<Change>> GroupIntoHunks(List<Change> changes)
    {
        var hunks = new List<List<Change>>();
        foreach (var change in changes)
        {
            var previous = hunks.Count > 0 ? hunks[^1][^1] : null;
            if (previous is not null && change.OldStart - (previous.OldStart + previous.OldCount) <= 2 * ContextLines)
            {
                hunks[^1].Add(change);
            }
            else
            {
                hunks.Add([change]);
            }
        }

        return hunks;
    }

    static void AppendHunk(StringBuilder builder, List<string> oldLines, List<string> newLines, List<Change> hunk)
    {
        var first = hunk[0];
        var last = hunk[^1];
        var oldStart = Math.Max(0, first.OldStart - ContextLines);
        var oldEnd = Math.Min(oldLines.Count, last.OldStart + last.OldCount + ContextLines);
        var newStart = first.NewStart - (first.OldStart - oldStart);
        var newCount = (oldEnd - oldStart) + hunk.Sum(change => change.NewCount - change.OldCount);

        builder.Append($"@@ -{oldStart + 1},{oldEnd - oldStart} +{newStart + 1},{newCount} @@\n");
        var cursor = oldStart;
        foreach (var change in hunk)
        {
            for (; cursor < change.OldStart; cursor++)
            {
                builder.Append(' ').Append(oldLines[cursor]).Append('\n');
            }

            for (var index = 0; index < change.OldCount; index++)
            {
                builder.Append('-').Append(oldLines[change.OldStart + index]).Append('\n');
            }

            for (var index = 0; index < change.NewCount; index++)
            {
                builder.Append('+').Append(newLines[change.NewStart + index]).Append('\n');
            }

            cursor = change.OldStart + change.OldCount;
        }

        for (; cursor < oldEnd; cursor++)
        {
            builder.Append(' ').Append(oldLines[cursor]).Append('\n');
        }
    }

    record Change(int OldStart, int OldCount, int NewStart, int NewCount);
}
