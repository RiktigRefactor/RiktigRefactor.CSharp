class Program
{
    static int Compute() => 1;

    static void Main()
    {
        var result = new { Value = Compute() };
        if (result.Value != 1)
        {
            throw new System.Exception("The anonymous object should hold the calculated value.");
        }
    }
}
