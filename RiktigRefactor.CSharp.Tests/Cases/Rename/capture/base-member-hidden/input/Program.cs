class Base
{
    public int Weight = 1;
}

class Derived : Base
{
    public int /*|*/Extra = 2;

    public int Total() => Weight + Extra;
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
