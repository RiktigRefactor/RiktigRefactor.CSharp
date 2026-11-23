public static class Orders
{
}

class Program
{
    static void Main()
    {
        var order = new Order();
        if (order.Double() != 8 || nameof(Order.Double) != "Double")
        {
            throw new System.Exception("Expected 8.");
        }
    }
}
