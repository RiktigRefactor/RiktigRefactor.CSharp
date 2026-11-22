using System.Linq;

class Program
{
    static void Main()
    {
        var total = (from /*|*/x in new[] { 1, 2, 3 } where x > 1 select x * 2).Sum();
        if (total != 10)
        {
            throw new System.Exception("The query should sum 4 and 6.");
        }
    }
}
