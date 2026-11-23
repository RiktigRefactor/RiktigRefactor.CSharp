class Counter(int count)
{
    void Bump() => count++;

    public int BumpAndRead()
    {
        var seen = 0;
        /*[*/Bump();
        seen = count;/*]*/
        return seen;
    }
}

class Program
{
    static void Main()
    {
        var seen = new Counter(0).BumpAndRead();
        if (seen != 1)
        {
            throw new System.Exception($"The count read after Bump should be 1, but was {seen}.");
        }
    }
}
