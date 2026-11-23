class Order
{
    public static int /*|*/Seed = System.Environment.ProcessorCount * 0 + 1;
}

class Pricing
{
}

class Program
{
    static void Main()
    {
        if (Order.Seed != 1)
        {
            throw new System.Exception("Expected 1.");
        }
    }
}
