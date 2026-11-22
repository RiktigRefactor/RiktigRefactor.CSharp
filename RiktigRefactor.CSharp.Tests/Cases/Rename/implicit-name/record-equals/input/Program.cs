record Money(int Amount)
{
    public virtual bool /*|*/Same(Money? other) => true;
}

class Program
{
    static void Main()
    {
        if (new Money(1) == new Money(2))
        {
            throw new System.Exception("Records with different amounts should not be equal.");
        }
    }
}
