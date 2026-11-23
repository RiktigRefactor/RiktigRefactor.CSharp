[System.AttributeUsage(System.AttributeTargets.Class)]
class TagAttribute : System.Attribute
{
    public int Value { get; set; }
}

[Tag(Value = 5)]
class Target
{
}

class Program
{
    static void Main()
    {
        var attribute = (TagAttribute)System.Attribute.GetCustomAttribute(typeof(Target), typeof(TagAttribute))!;
        if (attribute.Value != 5)
        {
            throw new System.Exception("The attribute's property should be 5.");
        }
    }
}
