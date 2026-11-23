class Program
{
    static int value = 1;

    static void Main()
    {
        System.Func<int, int> next = value => value + Program.value;
        if (next(10) != 11)
        {
            throw new System.Exception($"next(10) should be 11 but is {next(10)}.");
        }
    }
}
