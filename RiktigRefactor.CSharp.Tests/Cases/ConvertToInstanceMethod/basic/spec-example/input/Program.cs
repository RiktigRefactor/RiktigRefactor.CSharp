public class Order
{
    public decimal Price { get; set; } = 2;
}

public static class Pricing
{
    internal const decimal Discount = 1;

    public static decimal /*|*/Total(Order order, int qty) => order.Price * qty - Discount;
}

class Program
{
    static void Main()
    {
        var order = new Order();
        var t = Pricing.Total(order, 3);
        if (t != 5)
        {
            throw new System.Exception($"Expected 5, got {t}.");
        }
    }
}
