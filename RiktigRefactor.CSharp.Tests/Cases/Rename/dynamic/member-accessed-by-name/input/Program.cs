class Greeter
{
    public string /*|*/Hello() => "hi";
}

class Program
{
    static void Main()
    {
        dynamic greeter = new Greeter();
        if (greeter.Hello() != "hi")
        {
            throw new System.Exception("The dynamic call should succeed.");
        }
    }
}
