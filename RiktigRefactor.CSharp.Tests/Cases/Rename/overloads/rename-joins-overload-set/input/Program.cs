class Program
{
    static string F(int x) => "F-int";

    static string F(long x) => "F-long";

    static string /*|*/G(short x) => "G-short";

    static void Main()
    {
        if (G(1) != "G-short" || F(1) != "F-int")
        {
            throw new System.Exception("G(1) should call G, and F(1) should call F(int).");
        }
    }
}
