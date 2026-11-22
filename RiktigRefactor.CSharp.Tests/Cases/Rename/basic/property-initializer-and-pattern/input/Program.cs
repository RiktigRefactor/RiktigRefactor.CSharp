class Person
{
    public string /*|*/Name { get; init; } = "";
}

class Program
{
    static void Main()
    {
        var person = new Person { Name = "Ada" };
        if (person is not { Name: "Ada" })
        {
            throw new System.Exception("The property pattern should still match.");
        }
    }
}
