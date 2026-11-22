record Money(int /*|*/Amount);

class Program
{
    static void Main()
    {
        var money = new Money(Amount: 1);
        if (money.ToString() != "Money { Amount = 1 }")
        {
            throw new System.Exception("ToString should print the property name.");
        }
    }
}
