using System;

public class Order
{
    public int Quantity = 2;
}

public static class Orders
{
    public static int /*|*/Twice(Order order) => order.Quantity * 2;
}

class Program
{
    static void Main()
    {
        Func<Order, int> twice = Orders.Twice;
        if (twice(new Order()) != 4)
        {
            throw new Exception("Expected 4.");
        }
    }
}
