class Program
{
    static void Main()
    {
        var point = /*[*/new { X = 1, Y = 2 }/*]*/;
        if (point.X + point.Y != 3)
        {
            throw new System.Exception("The point should sum to 3.");
        }
    }
}
