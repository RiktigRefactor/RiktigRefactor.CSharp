struct Point
{
    public int X;

    public void Move() => this.X++;
}

class Program
{
    static void Main()
    {
        var point = new Point();
        /*[*/point/*]*/.Move();
        if (point.X != 1)
        {
            throw new System.Exception("The point should have moved.");
        }
    }
}
