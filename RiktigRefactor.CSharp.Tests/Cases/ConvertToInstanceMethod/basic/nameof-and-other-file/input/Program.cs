public static class Orders
{
    /// <summary>Like <see cref="Orders.Double"/>.</summary>
    public static int /*|*/Double(Order order) => order.Quantity * 2;
}

class Program
{
    static void Main()
    {
        var order = new Order();
        if (Orders.Double(order) != 8 || nameof(Orders.Double) != "Double")
        {
            throw new System.Exception("Expected 8.");
        }
    }
}
