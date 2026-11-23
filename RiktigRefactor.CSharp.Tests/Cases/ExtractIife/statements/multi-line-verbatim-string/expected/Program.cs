class Program
{
    static void Main()
    {
        string text = "";
        text = ((System.Func<string, string>)(static (text) =>
        {
            text = @"a
b";
            return text;
        }))(text);
        if (text != "a\nb" && text != "a\r\nb")
        {
            throw new System.Exception("The verbatim string should be unchanged.");
        }
    }
}
