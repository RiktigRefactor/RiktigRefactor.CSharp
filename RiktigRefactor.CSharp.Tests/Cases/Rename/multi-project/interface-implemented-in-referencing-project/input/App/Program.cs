using Lib;

IShape shape = new Square();
if (shape.Area() != 4)
{
    throw new System.Exception("wrong");
}

class Square : IShape
{
    public int Area() => 4;
}
