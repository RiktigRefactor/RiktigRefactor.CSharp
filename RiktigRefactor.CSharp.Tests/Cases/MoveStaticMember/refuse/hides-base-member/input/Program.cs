class Order
{
    public static string /*|*/Name() => "order";
}

class Base
{
    public static string Name() => "base";
}

class Pricing : Base
{
}

class Program
{
    static void Main()
    {
        if (Pricing.Name() != "base" || Order.Name() != "order")
        {
            throw new System.Exception("Wrong method.");
        }
    }
}
