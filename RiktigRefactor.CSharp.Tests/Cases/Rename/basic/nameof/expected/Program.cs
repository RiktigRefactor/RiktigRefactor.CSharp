class Item
{
    public int Quantity { get; set; }
}

class Program
{
    static void Main()
    {
        var property = typeof(Item).GetProperty(nameof(Item.Quantity));
        if (property is null || property.PropertyType != typeof(int))
        {
            throw new System.Exception("nameof must keep naming the property.");
        }
    }
}
