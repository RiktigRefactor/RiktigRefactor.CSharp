class Order
{
    decimal price = 2;

    public decimal /*|*/Total(int qty) => price * qty;

    public static decimal Total(Order order, int qty) => -1;
}

class Program
{
    static void Main()
    {
        if (new Order().Total(3) != 6)
        {
            throw new System.Exception("Expected 6.");
        }
    }
}
