public class Order
{
}

public static class Labels
{
    public static string /*|*/Describe(Order order, int count) => "count " + count;

    public static string Use(Order order) => Describe(order, 2);
}

class Program
{
    static void Main()
    {
        if (Labels.Use(null!) != "count 2")
        {
            throw new System.Exception("The body never uses the order.");
        }
    }
}
