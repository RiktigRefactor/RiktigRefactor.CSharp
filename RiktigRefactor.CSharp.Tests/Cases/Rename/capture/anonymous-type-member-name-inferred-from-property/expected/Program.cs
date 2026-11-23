class Person
{
    public string Title = "a";
}

class Program
{
    static void Main()
    {
        var person = new Person();
        var summary = new { Name = person.Title };
        if (summary.ToString() != "{ Name = a }")
        {
            throw new System.Exception("ToString should show the inferred member name.");
        }
    }
}
