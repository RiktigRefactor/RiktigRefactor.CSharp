class Program
{
    static void Main()
    {
        var name = "x";
        System.FormattableString text = /*[*/$"{name}!"/*]*/;
        if (text.ArgumentCount != 1)
        {
            throw new System.Exception("The FormattableString should have one argument.");
        }
    }
}
