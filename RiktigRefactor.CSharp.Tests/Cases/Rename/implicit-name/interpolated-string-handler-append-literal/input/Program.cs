using System.Runtime.CompilerServices;
using System.Text;

[InterpolatedStringHandler]
ref struct Handler
{
    readonly StringBuilder _text;

    public Handler(int literalLength, int formattedCount) => _text = new StringBuilder();

    public void /*|*/AppendLiteral(string value) => _text.Append(value.ToUpperInvariant());

    public void AppendFormatted<T>(T value) => _text.Append(value);

    public override string ToString() => _text.ToString();
}

class Program
{
    static string Format(Handler handler) => handler.ToString();

    static void Main()
    {
        if (Format($"a{1}b") != "A1B")
        {
            throw new System.Exception("The handler should upper-case literals.");
        }
    }
}
