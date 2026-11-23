class Box
{
    public int /*|*/Count = 1;
}

class Program
{
    static void Main()
    {
        var box = new Box();
        object inferred = new { box.Count };
        object named = new { Count = 1 };
        if (!inferred.Equals(named))
        {
            throw new System.Exception("Anonymous objects with the same member names and values should be equal.");
        }
    }
}
