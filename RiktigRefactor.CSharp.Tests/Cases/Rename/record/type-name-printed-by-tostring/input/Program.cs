record /*|*/Money(int Amount);

class Program
{
    static void Main()
    {
        if (new Money(1).ToString() != "Money { Amount = 1 }")
        {
            throw new System.Exception("ToString should print the record's name.");
        }
    }
}
