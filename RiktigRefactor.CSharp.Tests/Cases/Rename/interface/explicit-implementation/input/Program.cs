interface IShape
{
    int Area();
}

class Square : IShape
{
    int IShape./*|*/Area() => 4;
}

class Program
{
    static void Main()
    {
        IShape shape = new Square();
        if (shape.Area() != 4)
        {
            throw new System.Exception("Area should be 4 through the interface.");
        }
    }
}
