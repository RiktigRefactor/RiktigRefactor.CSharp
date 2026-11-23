using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var actions = new List<System.Action>();
        var count = 0;
        /*[*/actions.Add(() => count++);/*]*/
        actions[0]();
        if (count != 1)
        {
            throw new System.Exception($"The stored lambda should increment count, but it is {count}.");
        }
    }
}
