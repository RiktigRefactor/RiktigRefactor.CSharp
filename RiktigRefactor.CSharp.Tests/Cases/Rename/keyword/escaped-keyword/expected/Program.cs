class Program
{
    static void Main()
    {
        var @class = 3;
        if (@class + 1 != 4)
        {
            throw new System.Exception("item + 1 should be 4.");
        }
    }
}
