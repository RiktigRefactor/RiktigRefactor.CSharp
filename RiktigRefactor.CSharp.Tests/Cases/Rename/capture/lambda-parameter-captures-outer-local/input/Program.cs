using System;

class Program
{
    static void Main()
    {
        int offset = 10;
        Func<int, int> add = /*|*/n => n + offset;
        if (add(1) != 11)
        {
            throw new Exception("add(1) should be 11.");
        }
    }
}
