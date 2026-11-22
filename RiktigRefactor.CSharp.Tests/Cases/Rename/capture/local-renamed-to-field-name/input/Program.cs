using System.Collections.Generic;
using System.Linq;

class Calculator
{
    public static readonly List<string> Trace = [];

    int offset
    {
        get
        {
            Trace.Add("offset");
            return 100;
        }
    }

    public int Compute()
    {
        int /*|*/delta = 1;
        Trace.Add("delta");
        return delta + offset;
    }
}

class Program
{
    static void Main()
    {
        var result = new Calculator().Compute();
        if (result != 101 || !Calculator.Trace.SequenceEqual(["delta", "offset"]))
        {
            throw new System.Exception($"Unexpected result {result} or trace {string.Join(",", Calculator.Trace)}.");
        }
    }
}
