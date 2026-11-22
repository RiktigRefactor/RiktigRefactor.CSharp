class Items
{
    public int Length => 3;

    public int this[int index] => index;

    public int /*|*/Slice(int start, int length) => length;
}

class Program
{
    static void Main()
    {
        if (new Items()[1..] != 2)
        {
            throw new System.Exception("The range should call Slice.");
        }
    }
}
