class Item
{
    public string Tag = "item";
}

class Other
{
    public string Tag = "other";
}

class Box</*|*/T> where T : Other, new()
{
    public T Value = new T();

    public string Describe() => new Item().Tag;
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
