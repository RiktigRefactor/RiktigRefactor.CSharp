class Order
{
    public static int /*|*/Tally() => 1;
}

class Program
{
    static void Main()
    {
        if (Order.Tally() != 1)
        {
            throw new System.Exception("Expected 1.");
        }
    }
}
