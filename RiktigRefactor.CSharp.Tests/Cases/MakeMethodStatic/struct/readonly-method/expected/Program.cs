struct Point
{
    public int X;
    public int Y;

    public static int Sum(in Point point) => point.X + point.Y;
}

class Program
{
    static readonly Point Origin = new Point { X = 1, Y = 2 };

    static Point Make() => new Point { X = 3, Y = 4 };

    static void Main()
    {
        var p = new Point { X = 5, Y = 6 };
        var total = Point.Sum(p) + Point.Sum(Origin) + Point.Sum(Make());
        if (total != 21)
        {
            throw new System.Exception($"Expected 21, got {total}.");
        }
    }
}
