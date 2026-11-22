class Gadget
{
    public Gadget(int size)
    {
        Size = size;
    }

    public int Size { get; }
}

class Program
{
    static void Main()
    {
        var widget = new Gadget(3);
        if (widget.Size != 3)
        {
            throw new System.Exception("The constructor should set Size.");
        }
    }
}
