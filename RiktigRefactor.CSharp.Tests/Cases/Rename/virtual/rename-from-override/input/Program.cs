abstract class Shape
{
    public abstract string Name();
}

class Circle : Shape
{
    public override string Name() => "circle";
}

class Unit : Circle
{
    public override string /*|*/Name() => "unit " + base.Name();
}

class Program
{
    static void Main()
    {
        Shape shape = new Unit();
        if (shape.Name() != "unit circle")
        {
            throw new System.Exception("The most derived override should run and call its base.");
        }
    }
}
