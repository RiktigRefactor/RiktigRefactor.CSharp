class Program
{
    static void Main()
    {
        int @class = 2;
        /*[*/@class *= 21;/*]*/
        if (@class != 42)
        {
            throw new System.Exception($"Expected 42 but got {@class}.");
        }
    }
}
