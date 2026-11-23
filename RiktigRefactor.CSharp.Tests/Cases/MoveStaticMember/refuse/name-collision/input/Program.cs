class Order
{
    public static int /*|*/Rate() => 1;
}

class Pricing
{
    public static int Rate { get; } = 2;
}

class Program
{
    static void Main()
    {
        if (Order.Rate() + Pricing.Rate != 3)
        {
            throw new System.Exception("Expected 3.");
        }
    }
}
