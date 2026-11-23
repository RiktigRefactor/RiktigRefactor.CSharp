class Program
{
    static void Main()
    {
        var count = 0;
        /*[*/again:
        count++;
        if (count < 3)
        {
            goto again;
        }/*]*/

        if (count != 3)
        {
            throw new System.Exception("The count should be 3.");
        }
    }
}
