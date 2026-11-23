using System;

class Counter
{
    int value;

    public void /*|*/Accumulate(int amount)
    {
        value += amount;
        if (amount > 1)
        {
            Accumulate(amount - 1);
        }
    }

    public int AccumulateTwiceAndRead(int amount)
    {
        Accumulate(amount);
        this.Accumulate(amount);
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
