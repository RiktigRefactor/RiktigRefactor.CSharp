class Order
{
    public decimal Price = 4;

    public decimal /*|*/Double() => Price * 2;
}

class Program
{
    static void Main()
    {
        var o = new Order();
        if (o.Double() != 8)
        {
            throw new System.Exception("Expected 8.");
        }
    }
}
