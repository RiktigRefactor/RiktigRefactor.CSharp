[System.AttributeUsage(System.AttributeTargets.Class)]
class MyAttribute : System.Attribute
{
    public int /*|*/Value { get; set; }
}

[My(Value = 5)]
class Target
{
}

class Program
{
    static void Main()
    {
        var attribute = (MyAttribute)System.Attribute.GetCustomAttribute(typeof(Target), typeof(MyAttribute))!;
        if (attribute.Value != 5)
        {
            throw new System.Exception("The attribute's property should be 5.");
        }
    }
}
