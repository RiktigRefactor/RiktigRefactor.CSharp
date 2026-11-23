using System;
using System.Collections.Generic;

class Order
{
    decimal price = 2;
    public int Count { get; set; } = 3;
    public List<string> Log { get; } = new List<string>();

    decimal Discount() => 1;

    static string Describe(Order order) => order.Count.ToString();

    public static decimal Summarize(Order order)
    {
        Func<decimal> lambda = () => order.price * order.Count;
        decimal Local() => order.Discount();
        order.Log.Add(Describe(order));
        order.Count++;
        return lambda() - Local();
    }
}

class Program
{
    static void Main()
    {
        var order = new Order();
        var result = Order.Summarize(order);
        if (result != 7 || order.Count != 4 || order.Log[0] != "3")
        {
            throw new Exception($"Got {result}, {order.Count}, {order.Log[0]}.");
        }
    }
}
