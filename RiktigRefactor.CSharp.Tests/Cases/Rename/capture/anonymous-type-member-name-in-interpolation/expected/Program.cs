class Box
{
    public int Size = 1;
}

class Program
{
    static void Main()
    {
        var box = new Box();
        var text = $"{new { Count = box.Size }}";
        if (text != "{ Count = 1 }")
        {
            throw new System.Exception("The formatted anonymous type should show the inferred member name.");
        }
    }
}
