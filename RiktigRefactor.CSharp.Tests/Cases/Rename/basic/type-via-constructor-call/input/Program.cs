class Widget
{
    public Widget(int size)
    {
        Size = size;
    }

    public int Size { get; }
}

class Program
{
    static void Main()
    {
        var widget = new /*|*/Widget(3);
        Widget[] all = [widget];
        if (all[0].Size != 3)
        {
            throw new System.Exception("The constructor should set Size.");
        }
    }
}
