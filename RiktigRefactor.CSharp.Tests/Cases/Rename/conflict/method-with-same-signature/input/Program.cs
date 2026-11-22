class Program
{
    static int /*|*/A() => 1;

    static int B() => 2;

    static void Main()
    {
        if (A() + B() != 3)
        {
            throw new System.Exception("A() + B() should be 3.");
        }
    }
}
