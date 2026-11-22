class Counter
{
    int /*|*/count = 10;

    public int Get()
    {
        int total = 1;
        return total + count;
    }
}

class Program
{
    static void Main()
    {
        if (new Counter().Get() != 11)
        {
            throw new System.Exception("Get should add the field to the local.");
        }
    }
}
