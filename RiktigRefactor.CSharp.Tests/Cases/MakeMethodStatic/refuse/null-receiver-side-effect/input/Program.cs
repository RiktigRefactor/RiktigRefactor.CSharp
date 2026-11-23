using System;
using System.Collections.Generic;

class Order
{
    public static List<string> Trace = new List<string>();
    decimal price = 1;

    public decimal /*|*/Total(int qty)
    {
        Trace.Add("body");
        return price * qty;
    }
}

class Program
{
    static int Quantity()
    {
        Order.Trace.Add("argument");
        return 3;
    }

    static decimal Use(Order order) => order.Total(Quantity());

    static void Main()
    {
        try
        {
            Use(null!);
            throw new Exception("Expected a NullReferenceException.");
        }
        catch (NullReferenceException)
        {
        }

        if (string.Join(",", Order.Trace) != "argument")
        {
            throw new Exception(string.Join(",", Order.Trace));
        }
    }
}
