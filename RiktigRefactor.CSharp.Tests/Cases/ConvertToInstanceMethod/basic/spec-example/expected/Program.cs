public class Order
{
    public decimal Price { get; set; } = 2;

    public decimal Total(int qty) => this.Price * qty - Pricing.Discount;
}

public static class Pricing
{
    internal const decimal Discount = 1;
}

class Program
{
    static void Main()
    {
        var order = new Order();
        var t = order.Total(3);
        if (t != 5)
        {
            throw new System.Exception($"Expected 5, got {t}.");
        }
    }
}
