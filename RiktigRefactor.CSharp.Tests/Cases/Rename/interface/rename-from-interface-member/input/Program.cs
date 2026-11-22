interface IShape
{
    int /*|*/Area();
}

class Square : IShape
{
    public int Area() => 4;
}

class Program
{
    static void Main()
    {
        IShape shape = new Square();
        if (shape.Area() != 4 || new Square().Area() != 4)
        {
            throw new System.Exception("Area should be 4 through the interface and the class.");
        }
    }
}
