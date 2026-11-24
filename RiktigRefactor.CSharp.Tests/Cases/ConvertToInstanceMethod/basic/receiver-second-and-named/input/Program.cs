using System.Collections.Generic;

public class Order
{
    public decimal Price = 2;
}

public static class Pricing
{
    public static List<string> Trace = new List<string>();

    static int Quantity(int value)
    {
        Trace.Add("quantity");
        return value;
    }

    public static decimal /*|*/Total(int qty, Order order) => order.Price * qty;

    public static decimal Both()
    {
        var order = new Order();
        return Total(3, order) + Total(order: order, qty: Quantity(4)) + Total(qty: 5, order: new Order());
    }
}

class Program
{
    static void Main()
    {
        var total = Pricing.Both();
        if (total != 24 || string.Join(",", Pricing.Trace) != "quantity")
        {
            throw new System.Exception($"{total} {string.Join(",", Pricing.Trace)}");
        }
    }
}
