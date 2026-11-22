class Program
{
    static int F = 1;

    int Sum()
    {
        int F = 2;
        return F + Program.F;
    }

    static void Main()
    {
        if (new Program().Sum() != 3)
        {
            throw new System.Exception("Sum should add the local and the static field.");
        }
    }
}
