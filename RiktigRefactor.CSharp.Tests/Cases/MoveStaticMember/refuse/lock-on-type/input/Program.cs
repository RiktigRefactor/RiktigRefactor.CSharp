class Order
{
    static int count;

    public static int /*|*/Next()
    {
        lock (typeof(Order))
        {
            return ++Order.count;
        }
    }

    public static int Count => count;
}

class Pricing
{
}

class Program
{
    static void Main()
    {
        if (Order.Next() != 1)
        {
            throw new System.Exception("Expected 1.");
        }
    }
}
