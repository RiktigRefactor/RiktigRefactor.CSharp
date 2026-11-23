using System;
using System.Linq.Expressions;

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
        Expression<Func<Order, int>> twice = order => Orders.Twice(order);
        if (twice.Body.ToString() != "Twice(order)")
        {
            throw new Exception(twice.Body.ToString());
        }
    }
}
