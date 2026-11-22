interface IShape
{
    int Measure();
}

class Square : IShape
{
    public int Measure() => 4;
}

class Program
{
    static void Main()
    {
        IShape shape = new Square();
        if (shape.Measure() != 4 || new Square().Measure() != 4)
        {
            throw new System.Exception("Area should be 4 through the interface and the class.");
        }
    }
}
