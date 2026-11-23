class Program
{
    static void Main()
    {
        string text = "";
        /*[*/text = @"a
b";/*]*/
        if (text != "a\nb" && text != "a\r\nb")
        {
            throw new System.Exception("The verbatim string should be unchanged.");
        }
    }
}
