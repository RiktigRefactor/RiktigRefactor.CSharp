class Program
{
    static void Main()
    {
        int a = 20;
        /*[*/var doubled = a * 2;
        var answer = doubled + 2;/*]*/
        if (answer != 42)
        {
            throw new System.Exception($"Expected 42 but got {answer}.");
        }
    }
}
