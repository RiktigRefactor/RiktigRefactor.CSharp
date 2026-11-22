class Holder
{
    int stored;

    public int Stored
    {
        get => stored;
        set
        {
            int /*|*/bonus = 5;
            stored = value + bonus;
        }
    }
}

class Program
{
    static void Main()
    {
        var holder = new Holder();
        holder.Stored = 1;
        if (holder.Stored != 6)
        {
            throw new System.Exception("The setter should add the bonus.");
        }
    }
}
