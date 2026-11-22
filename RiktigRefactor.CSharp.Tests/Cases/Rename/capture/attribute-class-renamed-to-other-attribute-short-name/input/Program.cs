using System;

class SerialAttribute : Attribute
{
}

class /*|*/MarkAttribute : Attribute
{
}

[Mark]
class Target
{
}

class Program
{
    static void Main()
    {
        if (Attribute.GetCustomAttribute(typeof(Target), typeof(MarkAttribute)) is null)
        {
            throw new Exception("Target should have the Mark attribute.");
        }
    }
}
