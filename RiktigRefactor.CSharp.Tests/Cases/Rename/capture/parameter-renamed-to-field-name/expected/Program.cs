class Calculator
{
    int a = 5;

    public int Add(int a) => a + this.a;
}

class Program
{
    static void Main()
    {
        if (new Calculator().Add(1) != 6)
        {
            throw new System.Exception("Add(1) should be 6.");
        }
    }
}
