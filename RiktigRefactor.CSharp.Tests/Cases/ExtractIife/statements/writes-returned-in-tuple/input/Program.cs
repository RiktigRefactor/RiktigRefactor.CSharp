class Program
{
    static void Main()
    {
        int a = 1;
        int b = 0;
        /*[*/a++;
        b = a * 2;/*]*/
        if (a != 2 || b != 4)
        {
            throw new System.Exception($"Expected a=2, b=4 but got a={a}, b={b}.");
        }
    }
}
