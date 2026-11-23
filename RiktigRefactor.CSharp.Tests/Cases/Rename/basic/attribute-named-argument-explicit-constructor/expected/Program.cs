[System.AttributeUsage(System.AttributeTargets.Class)]
class MyAttribute : System.Attribute
{
    public MyAttribute(string label) => this.Label = label;

    public string Label { get; }

    public int Size { get; set; }
}

[My("x", Size = 5)]
class Target
{
}

class Program
{
    static void Main()
    {
        var attribute = (MyAttribute)System.Attribute.GetCustomAttribute(typeof(Target), typeof(MyAttribute))!;
        if (attribute.Label != "x" || attribute.Size != 5)
        {
            throw new System.Exception("The attribute should hold x and 5.");
        }
    }
}
