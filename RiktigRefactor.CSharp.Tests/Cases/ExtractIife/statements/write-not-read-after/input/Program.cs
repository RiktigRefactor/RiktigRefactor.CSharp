using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var log = new List<int>();
        int scratch;
        /*[*/scratch = 4;
        log.Add(scratch);/*]*/
        if (log[0] != 4)
        {
            throw new System.Exception("The log should hold 4.");
        }
    }
}
