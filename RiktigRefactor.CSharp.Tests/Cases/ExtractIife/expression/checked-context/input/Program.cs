class Program
{
    static void Main()
    {
        int a = int.MaxValue;
        int b = 1;
        try
        {
            var sum = checked(/*[*/a + b/*]*/);
            throw new System.Exception($"Expected an overflow, got {sum}.");
        }
        catch (System.OverflowException)
        {
        }
    }
}
