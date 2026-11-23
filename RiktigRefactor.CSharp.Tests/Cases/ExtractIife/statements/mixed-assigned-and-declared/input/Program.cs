class Program
{
    static void Main()
    {
        int count = 0;
        /*[*/count += 5;
        string label = "count " + count;/*]*/
        if (count != 5 || label != "count 5")
        {
            throw new System.Exception($"Expected 5 and \"count 5\" but got {count} and {label}.");
        }
    }
}
