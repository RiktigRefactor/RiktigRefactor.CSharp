class Order
{
    readonly decimal price;

    public Order(decimal price) => this.price = price;

    public static decimal Total(Order order, int qty) => order.price * qty;
}

class Program
{
    static void Main()
    {
        var order = new Order(2.5m);
        var t = Order.Total(order, 3);
        if (t != 7.5m)
        {
            throw new System.Exception($"Expected 7.5, got {t}.");
        }
    }
}
