class Program
{
    static void Main()
    {
        int result;
        int input = 6;
        /*[*/result = input * 7;/*]*/
        if (result != 42)
        {
            throw new System.Exception($"Expected 42 but got {result}.");
        }
    }
}
