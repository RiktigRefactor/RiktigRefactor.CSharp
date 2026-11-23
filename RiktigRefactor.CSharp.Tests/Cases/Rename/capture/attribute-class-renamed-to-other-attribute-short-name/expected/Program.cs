using System;

class SerialAttribute : Attribute
{
}

class Serial : Attribute
{
}

[@Serial]
class Target
{
}

class Program
{
    static void Main()
    {
        if (Attribute.GetCustomAttribute(typeof(Target), typeof(Serial)) is null)
        {
            throw new Exception("Target should have the Mark attribute.");
        }
    }
}
