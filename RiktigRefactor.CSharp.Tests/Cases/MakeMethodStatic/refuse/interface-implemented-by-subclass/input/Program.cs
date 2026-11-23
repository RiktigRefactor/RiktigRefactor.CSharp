interface INamed
{
    string Name();
}

class Base
{
    public string /*|*/Name() => "base";
}

class Derived : Base, INamed
{
}

class Program
{
    static void Main()
    {
        INamed named = new Derived();
        if (named.Name() != "base")
        {
            throw new System.Exception("Expected base.");
        }
    }
}
