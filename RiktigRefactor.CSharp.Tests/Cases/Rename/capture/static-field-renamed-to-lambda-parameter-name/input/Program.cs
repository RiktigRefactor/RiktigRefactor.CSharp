class Program
{
    static int /*|*/step = 1;

    static void Main()
    {
        System.Func<int, int> next = value => value + step;
        if (next(10) != 11)
        {
            throw new System.Exception($"next(10) should be 11 but is {next(10)}.");
        }
    }
}
