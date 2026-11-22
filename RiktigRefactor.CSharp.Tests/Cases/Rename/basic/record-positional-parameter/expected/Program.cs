record Point(int Horizontal, int Y);

class Program
{
    static void Main()
    {
        var point = new Point(1, 2);
        var moved = point with { Horizontal = 5 };
        var (x, y) = moved;
        if (x != 5 || y != 2 || point.Horizontal != 1)
        {
            throw new System.Exception("The with expression should update the renamed property.");
        }
    }
}
