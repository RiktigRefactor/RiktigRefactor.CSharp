using System;
using System.Linq.Expressions;

class Program
{
    static void Main()
    {
        Expression<Func<int, int>> increment = x => /*[*/x + 1/*]*/;
        if (increment.ToString() != "x => (x + 1)")
        {
            throw new Exception("The expression tree changed: " + increment);
        }
    }
}
