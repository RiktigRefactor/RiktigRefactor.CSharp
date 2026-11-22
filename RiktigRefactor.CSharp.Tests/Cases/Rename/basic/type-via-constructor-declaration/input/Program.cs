class Widget
{
    public /*|*/Widget(int size)
    {
        Size = size;
    }

    public int Size { get; }
}

class Program
{
    static void Main()
    {
        var widget = new Widget(3);
        if (widget.Size != 3)
        {
            throw new System.Exception("The constructor should set Size.");
        }
    }
}
