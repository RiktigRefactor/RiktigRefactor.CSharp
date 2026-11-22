namespace RiktigRefactor.CSharp.Tests.Infrastructure;

public static class SelectionMarkers
{
    public const string Cursor = "/*|*/";
    public const string RangeStart = "/*[*/";
    public const string RangeEnd = "/*]*/";

    /// <summary>Removes selection markers from every file, returning the clean text and the single selection.</summary>
    public static (IReadOnlyDictionary<string, string> Files, Selection Selection) Strip(IReadOnlyDictionary<string, string> files)
    {
        var cleanFiles = new Dictionary<string, string>(StringComparer.Ordinal);
        var selections = new List<Selection>();
        foreach (var (name, text) in files)
        {
            var (cleanText, selection) = StripOne(name, text);
            cleanFiles[name] = cleanText;
            if (selection is not null)
            {
                selections.Add(selection);
            }
        }

        if (selections.Count != 1)
        {
            throw new InvalidOperationException($"Expected exactly one selection marker across all input files, found {selections.Count}.");
        }

        return (cleanFiles, selections[0]);
    }

    static (string Text, Selection? Selection) StripOne(string name, string text)
    {
        var cursor = text.IndexOf(Cursor, StringComparison.Ordinal);
        var rangeStart = text.IndexOf(RangeStart, StringComparison.Ordinal);
        var rangeEnd = text.IndexOf(RangeEnd, StringComparison.Ordinal);

        if (cursor >= 0)
        {
            if (rangeStart >= 0 || rangeEnd >= 0 || text.IndexOf(Cursor, cursor + 1, StringComparison.Ordinal) >= 0)
            {
                throw new InvalidOperationException($"{name}: a file may contain only one selection.");
            }

            return (text.Remove(cursor, Cursor.Length), new Selection(name, cursor, cursor));
        }

        if (rangeStart < 0 && rangeEnd < 0)
        {
            return (text, null);
        }

        if (rangeStart < 0 || rangeEnd < rangeStart)
        {
            throw new InvalidOperationException($"{name}: unbalanced range markers.");
        }

        var withoutEnd = text.Remove(rangeEnd, RangeEnd.Length);
        var clean = withoutEnd.Remove(rangeStart, RangeStart.Length);
        return (clean, new Selection(name, rangeStart, rangeEnd - RangeStart.Length));
    }
}
