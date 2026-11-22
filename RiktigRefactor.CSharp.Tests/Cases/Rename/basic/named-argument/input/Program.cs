class Program
{
    static int Subtract(int /*|*/minuend, int subtrahend) => minuend - subtrahend;

    static void Main()
    {
        if (Subtract(subtrahend: 3, minuend: 10) != 7)
        {
            throw new System.Exception("Named arguments must still bind to the same parameters.");
        }
    }
}
