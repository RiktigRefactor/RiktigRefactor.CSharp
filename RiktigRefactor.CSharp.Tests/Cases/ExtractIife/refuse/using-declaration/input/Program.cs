using System.Collections.Generic;

class Resource : System.IDisposable
{
    readonly List<string> _log;

    public Resource(List<string> log) => this._log = log;

    public void Dispose() => this._log.Add("disposed");
}

class Program
{
    static void Main()
    {
        var log = new List<string>();
        Run(log);
        if (string.Join(",", log) != "body,after,disposed")
        {
            throw new System.Exception("Unexpected order: " + string.Join(",", log));
        }
    }

    static void Run(List<string> log)
    {
        /*[*/using var resource = new Resource(log);
        log.Add("body");/*]*/
        log.Add("after");
    }
}
