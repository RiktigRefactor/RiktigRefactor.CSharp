using System.Collections.Generic;

class Program
{
    static List<string> Log = new();

    static int Trace(string name, int value)
    {
        Log.Add(name);
        return value;
    }

    static void Main()
    {
        var sum = Trace("a", 1) + ((System.Func<int>)(static () => Trace("b", 2) * Trace("c", 3)))() + Trace("d", 4);
        if (sum != 11 || string.Join(",", Log) != "a,b,c,d")
        {
            throw new System.Exception($"Expected 11 and a,b,c,d but got {sum} and {string.Join(",", Log)}.");
        }
    }
}
