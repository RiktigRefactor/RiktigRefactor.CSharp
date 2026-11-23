class Animal
{
    public virtual string Sound() => "...";
}

class Dog : Animal
{
    public override string Sound() => "woof";

    public string /*|*/Both() => base.Sound() + Sound();
}

class Program
{
    static void Main()
    {
        if (new Dog().Both() != "...woof")
        {
            throw new System.Exception("Expected ...woof.");
        }
    }
}
