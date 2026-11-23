using System;

class Order
{
    public int Quantity = 2;

    public int /*|*/Twice() => Quantity * 2;
}

class Program
{
    static void Main()
    {
        var order = new Order();
        Func<int> twice = order.Twice;
        if (twice() != 4)
        {
            throw new Exception("Expected 4.");
        }
    }
}
