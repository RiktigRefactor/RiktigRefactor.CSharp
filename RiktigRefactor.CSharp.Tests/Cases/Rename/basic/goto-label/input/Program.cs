class Program
{
    static void Main()
    {
        var count = 0;
    /*|*/Loop:
        count++;
        if (count < 3)
        {
            goto Loop;
        }

        if (count != 3)
        {
            throw new System.Exception("The loop should run three times.");
        }
    }
}
