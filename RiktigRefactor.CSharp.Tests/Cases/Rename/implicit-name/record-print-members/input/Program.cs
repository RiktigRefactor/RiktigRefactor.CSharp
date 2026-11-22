using System.Text;

record Money(int Amount)
{
    protected virtual bool /*|*/Describe(StringBuilder builder)
    {
        builder.Append("custom");
        return true;
    }
}

class Program
{
    static void Main()
    {
        if (new Money(1).ToString() != "Money { Amount = 1 }")
        {
            throw new System.Exception("ToString should print the record's members.");
        }
    }
}
