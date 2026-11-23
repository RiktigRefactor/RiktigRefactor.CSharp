class Order
{
    public int /*|*/Tally() => 1;
}

class Program
{
    static void Main()
    {
        Order? order = null;
        if (order?.Tally() != null)
        {
            throw new System.Exception("Expected null.");
        }
    }
}
