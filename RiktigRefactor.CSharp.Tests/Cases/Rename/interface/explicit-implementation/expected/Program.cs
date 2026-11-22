interface IShape
{
    int Measure();
}

class Square : IShape
{
    int IShape.Measure() => 4;
}

class Program
{
    static void Main()
    {
        IShape shape = new Square();
        if (shape.Measure() != 4)
        {
            throw new System.Exception("Area should be 4 through the interface.");
        }
    }
}
