class Counter
{
    public Counter GetEnumerator() => this;

    int _remaining = 2;

    public bool MoveNext() => _remaining-- > 0;

    public int /*|*/Current => 5;
}

class Program
{
    static void Main()
    {
        var sum = 0;
        foreach (var item in new Counter())
        {
            sum += item;
        }

        if (sum != 10)
        {
            throw new System.Exception("foreach should add Current twice.");
        }
    }
}
