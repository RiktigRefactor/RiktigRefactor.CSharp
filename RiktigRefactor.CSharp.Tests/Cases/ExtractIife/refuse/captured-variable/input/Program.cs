using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var log = new List<int>();
        var n = 1;
        System.Action print = () => log.Add(n);
        /*[*/n = 5;
        print();/*]*/
        if (log[0] != 5 || n != 5)
        {
            throw new System.Exception($"The lambda should see n = 5, but logged {log[0]}.");
        }
    }
}
