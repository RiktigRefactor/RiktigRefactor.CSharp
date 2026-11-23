struct Counter
{
    public int Value;

    public int /*|*/Increment() => ++Value;
}

class Holder
{
    public readonly Counter Counter;

    public int IncrementCopy() => Counter.Increment();
}

class Program
{
    static void Main()
    {
        var holder = new Holder();
        holder.IncrementCopy();
        if (holder.Counter.Value != 0)
        {
            throw new System.Exception("The readonly field must not change.");
        }
    }
}
