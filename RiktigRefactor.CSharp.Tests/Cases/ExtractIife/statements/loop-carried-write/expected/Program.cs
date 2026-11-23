class Program
{
    static void Main()
    {
        int total = 0;
        for (var i = 1; i <= 4; i++)
        {
            total = ((System.Func<int, int, int>)(static (total, i) =>
            {
                total += i;
                return total;
            }))(total, i);
        }

        if (total != 10)
        {
            throw new System.Exception($"Expected 10 but got {total}.");
        }
    }
}
