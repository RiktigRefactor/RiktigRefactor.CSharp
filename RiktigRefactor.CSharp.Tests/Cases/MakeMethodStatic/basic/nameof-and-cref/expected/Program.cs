class Order
{
    int count = 1;

    /// <summary>Calls <see cref="Bump"/>.</summary>
    public static int Bump(Order order) => ++order.count;
}

class Program
{
    static void Main()
    {
        var order = new Order();
        var name = nameof(Order.Bump);
        if (name != "Bump" || Order.Bump(order) != 2)
        {
            throw new System.Exception(name);
        }
    }
}
