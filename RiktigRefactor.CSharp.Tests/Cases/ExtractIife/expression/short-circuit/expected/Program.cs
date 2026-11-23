class Program
{
    static int Calls;

    static bool Check(int value)
    {
        Calls++;
        return value > 0;
    }

    static void Main()
    {
        var value = -1;
        var result = value > 0 && ((System.Func<int, bool>)(static (value) => Check(value)))(value);
        if (result || Calls != 0)
        {
            throw new System.Exception("The right operand of && should not run.");
        }
    }
}
