using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var log = new List<int>();
        int a = 3;
        ((System.Action<List<int>, int>)(static (log, a) =>
        {
            log.Add(a);
            log.Add(a * 2);
        }))(log, a);
        if (string.Join(",", log) != "3,6")
        {
            throw new System.Exception("The log should be 3,6.");
        }
    }
}
