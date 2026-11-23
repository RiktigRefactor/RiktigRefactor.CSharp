using System.Collections.Generic;

static class Trace
{
    public static readonly List<string> Lines = new List<string>();
}

class Order
{
    static Order() => Trace.Lines.Add("Order initialized");

    public static int /*|*/One() => 1;
}

class Pricing
{
}

class Program
{
    static void Main()
    {
        Order.One();
        if (string.Join(",", Trace.Lines) != "Order initialized")
        {
            throw new System.Exception(string.Join(",", Trace.Lines));
        }
    }
}
