class Program
{
    static string Describe(long value) => "long " + value;

    static string Describe(int value) => "int " + value;

    static void Main()
    {
        int a = 20;
        int b = 22;
        long total = ((System.Func<int, int, int>)(static (a, b) => a + b))(a, b);
        if (total != 42 || Describe(a + b) != "int 42")
        {
            throw new System.Exception("The total should be 42.");
        }
    }
}
