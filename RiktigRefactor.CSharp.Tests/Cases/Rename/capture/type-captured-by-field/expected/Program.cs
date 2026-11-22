class Bar
{
    public static int Make() => 7;
}

class Other
{
    public int Make() => 1;
}

class Holder
{
    public Other Bar = new Other();

    public int Run() => global::Bar.Make();
}

class Program
{
    static void Main()
    {
        if (new Holder().Run() != 7)
        {
            throw new System.Exception("Run should call the static method on the type.");
        }
    }
}
