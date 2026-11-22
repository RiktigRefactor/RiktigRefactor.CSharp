class Point
{
    public void /*|*/Other(out int x, out int y) => (x, y) = (2, 2);
}

static class PointExtensions
{
    public static void Deconstruct(this Point point, out int x, out int y) => (x, y) = (1, 1);
}

class Program
{
    static void Main()
    {
        var (x, y) = new Point();
        if (x + y != 2)
        {
            throw new System.Exception("Deconstruction should use the extension Deconstruct.");
        }
    }
}
