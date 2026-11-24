public class Order
{
}

public class SpecialOrder : Order
{
    public string Label() => "special";
}

public static class Labels
{
    public static string /*|*/Label(Order order) => "plain";
}

class Program
{
    static void Main()
    {
        var special = new SpecialOrder();
        if (Labels.Label(special) != "plain" || special.Label() != "special")
        {
            throw new System.Exception("Wrong label.");
        }
    }
}
