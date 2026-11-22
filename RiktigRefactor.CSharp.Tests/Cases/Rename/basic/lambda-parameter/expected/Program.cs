using System;

class Program
{
    static void Main()
    {
        Func<int, int> triple = x => x * 3;
        if (triple(4) != 12)
        {
            throw new Exception("triple(4) should be 12.");
        }
    }
}
