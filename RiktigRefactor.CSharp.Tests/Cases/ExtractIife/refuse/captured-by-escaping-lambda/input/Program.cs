class Program
{
    static void Main()
    {
        var factor = 3;
        System.Func<int, int> times;
        /*[*/times = value => value * factor;/*]*/
        factor = 10;
        if (times(4) != 40)
        {
            throw new System.Exception($"The lambda should see factor = 10, but returned {times(4)}.");
        }
    }
}
