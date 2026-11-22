static class IntExtensions
{
    public static int Doubled(this int value) => value * 2;
}

class Program
{
    static void Main()
    {
        if (3.Doubled() != 6 || IntExtensions.Doubled(3) != 6)
        {
            throw new System.Exception("Twice should double the value in both call forms.");
        }
    }
}
