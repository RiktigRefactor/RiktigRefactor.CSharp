struct Counter
{
    public int Value;

    public int /*|*/Increment() => ++Value;
}

class Program
{
    static Counter Make() => new Counter();

    static void Main()
    {
        if (Make().Increment() != 1)
        {
            throw new System.Exception("Expected 1.");
        }
    }
}
