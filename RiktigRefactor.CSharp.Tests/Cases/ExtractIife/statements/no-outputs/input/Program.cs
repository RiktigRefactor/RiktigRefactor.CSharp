using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var log = new List<int>();
        int a = 3;
        /*[*/log.Add(a);
        log.Add(a * 2);/*]*/
        if (string.Join(",", log) != "3,6")
        {
            throw new System.Exception("The log should be 3,6.");
        }
    }
}
