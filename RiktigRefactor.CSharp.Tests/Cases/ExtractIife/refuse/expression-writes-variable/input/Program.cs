class Program
{
    static void Main()
    {
        var x = 1;
        var y = /*[*/x++/*]*/ + x;
        if (y != 3 || x != 2)
        {
            throw new System.Exception($"Expected y=3, x=2 but got y={y}, x={x}.");
        }
    }
}
