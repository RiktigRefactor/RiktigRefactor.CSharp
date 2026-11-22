abstract class Shape
{
    public abstract string Title();
}

class Circle : Shape
{
    public override string Title() => "circle";
}

class Unit : Circle
{
    public override string Title() => "unit " + base.Title();
}

class Program
{
    static void Main()
    {
        Shape shape = new Unit();
        if (shape.Title() != "unit circle")
        {
            throw new System.Exception("The most derived override should run and call its base.");
        }
    }
}
