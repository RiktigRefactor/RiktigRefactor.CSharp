class Order
{
    public int /*|*/Tally() => 1;
}

class Program
{
    static void Main()
    {
        var order = new Order();
#if NEVER
        order.Tally();
#endif
        if (order.Tally() != 1)
        {
            throw new System.Exception("Expected 1.");
        }
    }
}
