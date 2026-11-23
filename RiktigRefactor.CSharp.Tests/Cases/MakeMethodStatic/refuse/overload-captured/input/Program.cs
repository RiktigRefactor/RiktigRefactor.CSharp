class Order
{
    decimal price = 2;

    public decimal /*|*/Total(int qty) => price * qty;

    public static decimal Total(object order, int qty) => -1;
}

class Program
{
    static void Main()
    {
        var order = new Order();
        if (Order.Total(order, 3) != -1 || order.Total(3) != 6)
        {
            throw new System.Exception("Wrong overload.");
        }
    }
}
