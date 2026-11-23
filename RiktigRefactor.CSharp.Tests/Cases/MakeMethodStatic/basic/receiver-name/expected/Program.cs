class Order
{
    public decimal Price = 4;

    public static decimal Double(Order self) => self.Price * 2;
}

class Program
{
    static void Main()
    {
        var o = new Order();
        if (Order.Double(o) != 8)
        {
            throw new System.Exception("Expected 8.");
        }
    }
}
