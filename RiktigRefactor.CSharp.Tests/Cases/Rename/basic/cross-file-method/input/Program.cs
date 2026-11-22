class Program
{
    static void Main()
    {
        if (Calculator.Double(21) != 42)
        {
            throw new System.Exception("Double(21) should be 42.");
        }
    }
}
