class Program
{
    static int /*|*/Calculate() => 1;

    static void Main()
    {
        var result = new { Value = Calculate() };
        if (result.Value != 1)
        {
            throw new System.Exception("The anonymous object should hold the calculated value.");
        }
    }
}
