using System;

class Counter
{
    int value;

    public static void Accumulate(Counter counter, int amount)
    {
        counter.value += amount;
        if (amount > 1)
        {
            Counter.Accumulate(counter, amount - 1);
        }
    }

    public int AccumulateTwiceAndRead(int amount)
    {
        Counter.Accumulate(this, amount);
        Counter.Accumulate(this, amount);
        return value;
    }
}

class Program
{
    static void Main()
    {
        var result = new Counter().AccumulateTwiceAndRead(3);
        if (result != 12)
        {
            throw new Exception($"Expected 12, got {result}.");
        }
    }
}
