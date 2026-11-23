using System.Collections.Generic;

static class Trace
{
    public static readonly List<string> Lines = new List<string>();
}

class Order
{
    public static int /*|*/One() => 1;
}

class Pricing
{
    static Pricing() => Trace.Lines.Add("Pricing initialized");
}

class Program
{
    static void Main()
    {
        Order.One();
        if (Trace.Lines.Count != 0)
        {
            throw new System.Exception(string.Join(",", Trace.Lines));
        }
    }
}
