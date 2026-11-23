public class Order
{
}

public static class Orders
{
    public static int /*|*/One(Order order) => 1;
}

class Program
{
    static void Main()
    {
        if (Orders.One(new Order()) != 1)
        {
            throw new System.Exception("Expected 1.");
        }
    }
}
