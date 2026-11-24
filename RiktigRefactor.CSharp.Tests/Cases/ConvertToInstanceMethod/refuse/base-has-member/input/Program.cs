public class Item
{
    public string Label() => "item";
}

public class Order : Item
{
}

public static class Labels
{
    public static string /*|*/Label(Order order) => "order";
}

class Program
{
    static void Main()
    {
        if (Labels.Label(new Order()) != "order" || new Order().Label() != "item")
        {
            throw new System.Exception("Wrong label.");
        }
    }
}
