class Order
{
    int count = 1;

    /// <summary>Calls <see cref="Bump"/>.</summary>
    public int /*|*/Bump() => ++count;
}

class Program
{
    static void Main()
    {
        var order = new Order();
        var name = nameof(Order.Bump);
        if (name != "Bump" || order.Bump() != 2)
        {
            throw new System.Exception(name);
        }
    }
}
