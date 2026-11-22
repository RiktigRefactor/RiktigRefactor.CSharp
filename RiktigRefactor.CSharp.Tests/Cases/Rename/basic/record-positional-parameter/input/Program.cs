record Point(int /*|*/X, int Y);

class Program
{
    static void Main()
    {
        var point = new Point(1, 2);
        var moved = point with { X = 5 };
        var (x, y) = moved;
        if (x != 5 || y != 2 || point.X != 1)
        {
            throw new System.Exception("The with expression should update the renamed property.");
        }
    }
}
