using System;

class Source
{
    public Source /*|*/Other(Func<int, int> selector) => new Source { Value = 2 };

    public int Value { get; init; }
}

static class SourceExtensions
{
    public static Source Select(this Source source, Func<int, int> selector) => new Source { Value = 1 };
}

class Program
{
    static void Main()
    {
        var result = from item in new Source() select item;
        if (result.Value != 1)
        {
            throw new Exception("The query should use the extension Select.");
        }
    }
}
