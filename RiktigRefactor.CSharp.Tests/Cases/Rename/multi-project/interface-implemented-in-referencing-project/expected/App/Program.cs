using Lib;

IShape shape = new Square();
if (shape.Size() != 4)
{
    throw new System.Exception("wrong");
}

class Square : IShape
{
    public int Size() => 4;
}
