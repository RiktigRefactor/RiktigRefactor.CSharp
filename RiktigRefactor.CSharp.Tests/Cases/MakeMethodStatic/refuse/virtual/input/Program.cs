class Animal
{
    public virtual string /*|*/Sound() => "...";
}

class Dog : Animal
{
    public override string Sound() => "woof";
}

class Program
{
    static void Main()
    {
        Animal animal = new Dog();
        if (animal.Sound() != "woof")
        {
            throw new System.Exception("Expected woof.");
        }
    }
}
