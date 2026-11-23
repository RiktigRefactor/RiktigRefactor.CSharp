class Account
{
    decimal _rate = 0.5m;

    decimal Rate => this._rate;

    public decimal Interest(decimal amount) => ((System.Func<Account, decimal, decimal>)(static (@this, amount) => @this.Rate * amount + @this._rate))(this, amount);
}

class Program
{
    static void Main()
    {
        if (new Account().Interest(10m) != 5.5m)
        {
            throw new System.Exception("The interest should be 5.5.");
        }
    }
}
