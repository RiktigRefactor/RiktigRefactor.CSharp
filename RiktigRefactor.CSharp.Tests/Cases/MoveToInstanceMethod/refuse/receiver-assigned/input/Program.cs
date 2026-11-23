public class Order
{
    public int Quantity = 2;
}

public static class Orders
{
    public static int /*|*/Fresh(Order order)
    {
        order = new Order();
        return order.Quantity;
    }
}

class Program
{
    static void Main()
    {
        var order = new Order { Quantity = 5 };
        if (Orders.Fresh(order) != 2)
        {
            throw new System.Exception("Expected 2.");
        }
    }
}
