class Program
{
    static void Main()
    {
        var count = 0;
        while (true)
        {
            count++;
            /*[*/if (count == 3)
            {
                break;
            }/*]*/
        }

        if (count != 3)
        {
            throw new System.Exception("The loop should stop at 3.");
        }
    }
}
