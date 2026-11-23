class Program
{
    static void Main()
    {
        var offset = 1;
        /*[*/var scale = 2;
        System.Func<int, int> times = value => value * scale;
        scale = 3;
        var result = times(4) + offset;/*]*/
        if (result != 13)
        {
            throw new System.Exception($"The result should be 13 but is {result}.");
        }
    }
}
