class Program
{
    static void Main()
    {
        var numbers = new int[1];
        ref int first = ref numbers[0];
        var seen = 0;
        /*[*/numbers[0] = 5;
        seen = first;/*]*/
        if (seen != 5)
        {
            throw new System.Exception($"The ref local should see 5, but saw {seen}.");
        }
    }
}
