static class IntExtensions
{
    public static int /*|*/Twice(this int value) => value * 2;
}

class Program
{
    static void Main()
    {
        if (3.Twice() != 6 || IntExtensions.Twice(3) != 6)
        {
            throw new System.Exception("Twice should double the value in both call forms.");
        }
    }
}
