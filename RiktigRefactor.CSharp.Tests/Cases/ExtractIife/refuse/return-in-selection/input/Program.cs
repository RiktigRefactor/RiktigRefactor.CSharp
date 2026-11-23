class Program
{
    static int Sign(int value)
    {
        /*[*/if (value < 0)
        {
            return -1;
        }/*]*/

        return 1;
    }

    static void Main()
    {
        if (Sign(-5) != -1)
        {
            throw new System.Exception("Sign(-5) should be -1.");
        }
    }
}
