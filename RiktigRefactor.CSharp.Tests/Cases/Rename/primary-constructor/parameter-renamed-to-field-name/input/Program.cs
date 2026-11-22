class Account(int /*|*/balance)
{
    int total = 5;

    public int Balance() => balance;

    public int Total() => total;
}

class Program
{
    static void Main()
    {
        var account = new Account(1);
        if (account.Balance() != 1 || account.Total() != 5)
        {
            throw new System.Exception("Balance should read the parameter.");
        }
    }
}
