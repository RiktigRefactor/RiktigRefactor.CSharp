class Animal
{
    public virtual string MakeNoise() => "...";
}

class Dog : Animal
{
    public override string MakeNoise() => "woof";
}

class Program
{
    static void Main()
    {
        Animal animal = new Dog();
        if (animal.MakeNoise() != "woof")
        {
            throw new System.Exception("The override should run.");
        }
    }
}
