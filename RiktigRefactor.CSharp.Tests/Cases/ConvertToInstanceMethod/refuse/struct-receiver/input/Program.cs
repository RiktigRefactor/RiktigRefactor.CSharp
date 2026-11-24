public struct Counter
{
    public int Value;
}

public static class Counters
{
    public static int /*|*/Next(Counter counter)
    {
        counter.Value++;
        return counter.Value;
    }
}

class Program
{
    static void Main()
    {
        var counter = new Counter();
        Counters.Next(counter);
        if (counter.Value != 0)
        {
            throw new System.Exception("The argument was a copy.");
        }
    }
}
