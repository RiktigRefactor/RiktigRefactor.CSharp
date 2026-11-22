using Microsoft.CSharp.RuntimeBinder;

class Greeter
{
    public string /*|*/Hello() => "hi";
}

class Program
{
    static void Main()
    {
        dynamic greeter = new Greeter();
        var failed = false;
        try
        {
            greeter.Greet();
        }
        catch (RuntimeBinderException)
        {
            failed = true;
        }

        if (!failed || new Greeter().Hello() != "hi")
        {
            throw new System.Exception("Greet should not exist yet.");
        }
    }
}
