struct Counter
{
    public int Value;

    public void Increment() => this.Value++;
}

class Program
{
    static void Main()
    {
        var counter = new Counter();
        counter = ((System.Func<Counter, Counter>)(static (counter) =>
        {
            counter.Increment();
            counter.Increment();
            return counter;
        }))(counter);
        if (counter.Value != 2)
        {
            throw new System.Exception($"The counter should be 2 but is {counter.Value}.");
        }
    }
}
