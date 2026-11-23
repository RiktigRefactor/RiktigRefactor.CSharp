using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var log = new List<int>();
        int scratch;
        ((System.Action<List<int>>)(static (log) =>
        {
            int scratch;
            scratch = 4;
            log.Add(scratch);
        }))(log);
        if (log[0] != 4)
        {
            throw new System.Exception("The log should hold 4.");
        }
    }
}
