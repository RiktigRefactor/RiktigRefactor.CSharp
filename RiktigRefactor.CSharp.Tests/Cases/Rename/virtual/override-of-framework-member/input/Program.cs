class Money
{
    public override string /*|*/ToString() => "$5";
}

class Program
{
    static void Main()
    {
        object money = new Money();
        if (money.ToString() != "$5")
        {
            throw new System.Exception("ToString should be overridden.");
        }
    }
}
