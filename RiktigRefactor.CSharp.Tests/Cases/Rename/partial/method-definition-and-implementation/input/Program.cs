partial class Calculator
{
    public partial int Value();
}

partial class Calculator
{
    public partial int /*|*/Value() => 3;
}

class Program
{
    static void Main()
    {
        if (new Calculator().Value() != 3)
        {
            throw new System.Exception("Value should come from the implementation.");
        }
    }
}
