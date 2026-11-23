class Order
{
    static int secret = 7;

    public static int /*|*/Reveal() => secret;
}

class Pricing
{
}

class Program
{
    static void Main()
    {
        if (Order.Reveal() != 7)
        {
            throw new System.Exception("Expected 7.");
        }
    }
}
