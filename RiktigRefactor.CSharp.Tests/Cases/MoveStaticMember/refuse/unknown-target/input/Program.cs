class Order
{
    public static int /*|*/One() => 1;
}

class Program
{
    static void Main()
    {
        if (Order.One() != 1)
        {
            throw new System.Exception("Expected 1.");
        }
    }
}
