struct Counter
{
    public int Value;

    public static void Increment(ref Counter counter, int by) => counter.Value += by;

    public void IncrementTwice()
    {
        Counter.Increment(ref this, 1);
        Counter.Increment(ref this, 1);
    }
}

class Program
{
    static void Main()
    {
        var counter = new Counter();
        Counter.Increment(ref counter, 5);
        counter.IncrementTwice();
        var counters = new Counter[1];
        Counter.Increment(ref counters[0], 3);
        if (counter.Value != 7 || counters[0].Value != 3)
        {
            throw new System.Exception($"{counter.Value} {counters[0].Value}");
        }
    }
}
