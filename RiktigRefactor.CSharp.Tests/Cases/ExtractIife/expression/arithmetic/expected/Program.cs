class Program
{
    static void Main()
    {
        decimal price = 2.5m;
        int qty = 4;
        var total = ((System.Func<decimal, int, decimal>)(static (price, qty) => price * qty))(price, qty);
        if (total != 10m)
        {
            throw new System.Exception("The total should be 10.");
        }
    }
}
