class Counter
{
    public int Value;

    public bool MoveNext() => ++this.Value < 3;
}

class Program
{
    static void Main()
    {
        var total = 0;
        foreach (var number in new[] { 1, 2, 3 })
        {
            total += number;
        }

        var counter = new Counter();
        while (counter.MoveNext())
        {
        }

        if (total != 6 || counter.Value != 3)
        {
            throw new System.Exception("The loop should sum to 6 and the counter reach 3.");
        }
    }
}
