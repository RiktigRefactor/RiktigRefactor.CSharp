class Program
{
    static void Fail() => throw new System.InvalidOperationException();

    static void Main()
    {
        var step = 0;
        try
        {
            /*[*/step = 1;
            Fail();/*]*/
        }
        catch (System.InvalidOperationException)
        {
        }

        if (step != 1)
        {
            throw new System.Exception($"The step should be 1 but is {step}.");
        }
    }
}
