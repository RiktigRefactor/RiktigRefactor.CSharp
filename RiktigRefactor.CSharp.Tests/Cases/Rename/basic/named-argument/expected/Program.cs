class Program
{
    static int Subtract(int start, int subtrahend) => start - subtrahend;

    static void Main()
    {
        if (Subtract(subtrahend: 3, start: 10) != 7)
        {
            throw new System.Exception("Named arguments must still bind to the same parameters.");
        }
    }
}
