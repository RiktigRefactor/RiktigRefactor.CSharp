class Person
{
    public string /*|*/Name = "a";
}

class Program
{
    static void Main()
    {
        var person = new Person();
        var summary = new { person.Name };
        if (summary.ToString() != "{ Name = a }")
        {
            throw new System.Exception("ToString should show the inferred member name.");
        }
    }
}
