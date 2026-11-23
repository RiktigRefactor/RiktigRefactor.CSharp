class Program
{
    static void Main()
    {
        byte small = /*[*/200/*]*/;
        if (small != 200)
        {
            throw new System.Exception("The byte should be 200.");
        }
    }
}
