class Counter<T>
{
    static int /*|*/step = 1;

    public static int Next(int start)
    {
        System.Func<int, int> next = value => value + step;
        return next(start);
    }
}

class Program
{
    static void Main()
    {
        if (Counter<string>.Next(10) != 11)
        {
            throw new System.Exception("Next(10) should be 11.");
        }
    }
}
