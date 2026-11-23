using System;
using System.Linq.Expressions;

class Order
{
    public int /*|*/Tally(int extra) => extra;
}

class Program
{
    static void Main()
    {
        Expression<Func<Order, int>> count = order => order.Tally(1);
        if (count.Body.ToString() != "order.Tally(1)")
        {
            throw new Exception(count.Body.ToString());
        }
    }
}
