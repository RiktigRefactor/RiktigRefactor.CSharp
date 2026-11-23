class Order
{
    public static int /*|*/One() => 1;
}

class Pricing
{
}

class Program
{
    static void Main()
    {
#if NEVER
        Order.One();
#endif
        if (Order.One() != 1)
        {
            throw new System.Exception("Expected 1.");
        }
    }
}
