struct Counter
{
    public int Value;

    public void Twice()
    {
        /*[*/Value++;
        Value++;/*]*/
    }
}

class Program
{
    static void Main()
    {
        var counter = new Counter();
        counter.Twice();
        if (counter.Value != 2)
        {
            throw new System.Exception($"The counter should be 2 but is {counter.Value}.");
        }
    }
}
