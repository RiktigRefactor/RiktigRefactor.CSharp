class Counter
{
    int total = 10;

    public int Get()
    {
        int total = 1;
        return total + this.total;
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
