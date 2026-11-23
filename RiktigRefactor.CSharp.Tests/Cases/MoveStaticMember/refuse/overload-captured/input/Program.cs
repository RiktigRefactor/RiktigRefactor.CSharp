class Order
{
    public static string /*|*/Describe(int value) => "moved";
}

class Pricing
{
    public static string Describe(long value) => "long";

    public static string Call() => Describe(1);
}

class Program
{
    static void Main()
    {
        if (Pricing.Call() != "long" || Order.Describe(1) != "moved")
        {
            throw new System.Exception("Wrong overload.");
        }
    }
}
