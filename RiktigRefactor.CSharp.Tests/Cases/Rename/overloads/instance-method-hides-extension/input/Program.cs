class Box
{
    public string Look() => "instance";
}

static class BoxExtensions
{
    public static string /*|*/Peek(this Box box) => "extension";
}

class Program
{
    static void Main()
    {
        var box = new Box();
        if (box.Peek() != "extension")
        {
            throw new System.Exception("Peek should call the extension method.");
        }
    }
}
