class Program
{
    static int Seven() => 7;

    static void Main()
    {
        var value = ((System.Func<int>)(static () => Seven() * 6))() + 0;
        if (value != 42)
        {
            throw new System.Exception("The value should be 42.");
        }
    }
}
