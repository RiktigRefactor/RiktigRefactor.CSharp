class Items
{
    public int Count => 2;

    public int /*|*/Size { get; } = 3;

    public int this[int index] => index;
}

class Program
{
    static void Main()
    {
        if (new Items() is not [_, _])
        {
            throw new System.Exception("The list pattern should use Count.");
        }
    }
}
