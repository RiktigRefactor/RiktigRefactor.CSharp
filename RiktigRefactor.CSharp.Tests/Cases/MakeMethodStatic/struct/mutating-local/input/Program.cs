struct Counter
{
    public int Value;

    public void /*|*/Increment(int by) => Value += by;

    public void IncrementTwice()
    {
        Increment(1);
        Increment(1);
    }
}

class Program
{
    static void Main()
    {
        var counter = new Counter();
        counter.Increment(5);
        counter.IncrementTwice();
        var counters = new Counter[1];
        counters[0].Increment(3);
        if (counter.Value != 7 || counters[0].Value != 3)
        {
            throw new System.Exception($"{counter.Value} {counters[0].Value}");
        }
    }
}
