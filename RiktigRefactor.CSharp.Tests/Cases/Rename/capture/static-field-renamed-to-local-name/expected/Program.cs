class Program
{
    static int total = 1;

    static void Main()
    {
        var total = 10;
        total += Program.total;
        if (total != 11)
        {
            throw new System.Exception($"The total should be 11 but is {total}.");
        }
    }
}
