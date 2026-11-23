class Order
{
    public int /*|*/Quantity() => 1;
}

class Pricing
{
}

class Program
{
    static void Main()
    {
        if (new Order().Quantity() != 1)
        {
            throw new System.Exception("Expected 1.");
        }
    }
}
