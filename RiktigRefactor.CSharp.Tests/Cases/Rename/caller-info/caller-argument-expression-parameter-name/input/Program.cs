using System.Runtime.CompilerServices;

class Program
{
    static string Capture(int /*|*/value, [CallerArgumentExpression("value")] string expression = "") => expression;

    static void Main()
    {
        if (Capture(1 + 2) != "1 + 2")
        {
            throw new System.Exception("The argument expression should be captured.");
        }
    }
}
