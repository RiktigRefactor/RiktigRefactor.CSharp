class Program
{
    static void Main()
    {
        var total = 0;
        void Add(int amount) => total += amount;
        var amount = 2;
        /*[*/Add(amount);
        Add(amount);/*]*/
        if (total != 4)
        {
            throw new System.Exception($"The total should be 4 but is {total}.");
        }
    }
}
