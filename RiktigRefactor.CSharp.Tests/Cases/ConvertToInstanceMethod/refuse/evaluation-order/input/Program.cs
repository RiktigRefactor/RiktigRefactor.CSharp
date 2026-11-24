using System.Collections.Generic;

public class Order
{
    public Order(string name) => Pricing.Trace.Add(name);

    public decimal Price = 2;
}

public static class Pricing
{
    public static List<string> Trace = new List<string>();

    static int Quantity()
    {
        Trace.Add("quantity");
        return 3;
    }

    public static decimal /*|*/Total(int qty, Order order) => order.Price * qty;

    public static decimal Use() => Total(Quantity(), new Order("order"));
}

class Program
{
    static void Main()
    {
        Pricing.Use();
        if (string.Join(",", Pricing.Trace) != "quantity,order")
        {
            throw new System.Exception(string.Join(",", Pricing.Trace));
        }
    }
}
