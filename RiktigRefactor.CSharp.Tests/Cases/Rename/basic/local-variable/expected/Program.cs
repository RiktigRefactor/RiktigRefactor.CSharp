class Program
{
    static void Main()
    {
        var sum = 0;
        for (var i = 1; i <= 4; i++)
        {
            sum += i;
        }

        if (sum != 10)
        {
            throw new System.Exception($"Expected 10 but got {sum}.");
        }
    }
}
