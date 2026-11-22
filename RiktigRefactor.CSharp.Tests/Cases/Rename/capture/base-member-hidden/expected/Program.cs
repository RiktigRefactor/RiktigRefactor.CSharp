class Base
{
    public int Weight = 1;
}

class Derived : Base
{
    public int Weight = 2;

    public int Total() => base.Weight + Weight;
}

class Program
{
    static void Main()
    {
        if (new Derived().Total() != 3)
        {
            throw new System.Exception("Total should add the base member and the derived member.");
        }
    }
}
