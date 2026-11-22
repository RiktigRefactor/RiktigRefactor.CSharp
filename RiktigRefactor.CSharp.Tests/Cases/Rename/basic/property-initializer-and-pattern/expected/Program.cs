class Person
{
    public string FullName { get; init; } = "";
}

class Program
{
    static void Main()
    {
        var person = new Person { FullName = "Ada" };
        if (person is not { FullName: "Ada" })
        {
            throw new System.Exception("The property pattern should still match.");
        }
    }
}
