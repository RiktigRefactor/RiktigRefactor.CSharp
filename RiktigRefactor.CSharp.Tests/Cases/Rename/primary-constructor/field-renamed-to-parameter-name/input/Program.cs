class Account(int balance)
{
    int /*|*/total = 5;

    public int Sum() => balance + total * 10;
}

class Program
{
    static void Main()
    {
        if (new Account(1).Sum() != 51)
        {
            throw new System.Exception("Sum should read both the parameter and the field.");
        }
    }
}
