class Item
{
    public string Tag = "item";
}

class Other
{
    public string Tag = "other";
}

class Box<Item> where Item : Other, new()
{
    public Item Value = new Item();

    public string Describe() => new global::Item().Tag;
}

class Program
{
    static void Main()
    {
        if (new Box<Other>().Describe() != "item")
        {
            throw new System.Exception("Describe should create an Item.");
        }
    }
}
