using System.Linq;

class Program
{
    static void Main()
    {
        var total = (from n in new[] { 1, 2, 3 } where n > 1 select n * 2).Sum();
        if (total != 10)
        {
            throw new System.Exception("The query should sum 4 and 6.");
        }
    }
}
