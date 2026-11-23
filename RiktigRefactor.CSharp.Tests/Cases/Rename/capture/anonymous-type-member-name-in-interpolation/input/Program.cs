class Box
{
    public int /*|*/Count = 1;
}

class Program
{
    static void Main()
    {
        var box = new Box();
        var text = $"{new { box.Count }}";
        if (text != "{ Count = 1 }")
        {
            throw new System.Exception("The formatted anonymous type should show the inferred member name.");
        }
    }
}
