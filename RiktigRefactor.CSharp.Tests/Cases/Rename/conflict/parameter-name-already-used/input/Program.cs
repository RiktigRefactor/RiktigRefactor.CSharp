class Program
{
    static int Subtract(int /*|*/left, int right) => left - right;

    static void Main()
    {
        if (Subtract(5, 3) != 2)
        {
            throw new System.Exception("Subtract(5, 3) should be 2.");
        }
    }
}
