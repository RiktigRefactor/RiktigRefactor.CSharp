class Counter
{
    public int Twice() => 1;
}

static class CounterExtensions
{
    extension(Counter counter)
    {
        public int /*|*/Doubled() => 2;
    }
}

class Program
{
    static void Main()
    {
        if (new Counter().Doubled() != 2)
        {
            throw new System.Exception("The extension member should be called.");
        }
    }
}
