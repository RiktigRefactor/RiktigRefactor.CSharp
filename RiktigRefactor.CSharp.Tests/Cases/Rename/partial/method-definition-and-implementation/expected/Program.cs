partial class Calculator
{
    public partial int Compute();
}

partial class Calculator
{
    public partial int Compute() => 3;
}

class Program
{
    static void Main()
    {
        if (new Calculator().Compute() != 3)
        {
            throw new System.Exception("Value should come from the implementation.");
        }
    }
}
