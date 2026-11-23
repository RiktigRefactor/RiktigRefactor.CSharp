class Counter
{
    int _count;

    public int Count => this._count;

    void Bump() => this._count++;

    public void Twice()
    {
        /*[*/Bump();
        this.Bump();/*]*/
    }
}

class Program
{
    static void Main()
    {
        var counter = new Counter();
        counter.Twice();
        if (counter.Count != 2)
        {
            throw new System.Exception("The count should be 2.");
        }
    }
}
