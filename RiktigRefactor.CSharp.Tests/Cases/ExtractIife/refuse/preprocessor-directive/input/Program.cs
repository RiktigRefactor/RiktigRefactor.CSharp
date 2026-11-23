class Program
{
    static void Main()
    {
        var value = 0;
        /*[*/value++;
#if DEBUG
        value++;
#endif
        value++;/*]*/
        if (value != 2)
        {
            throw new System.Exception("value should be 2.");
        }
    }
}
