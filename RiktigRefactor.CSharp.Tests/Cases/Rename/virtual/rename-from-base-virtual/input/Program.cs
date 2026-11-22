class Animal
{
    public virtual string /*|*/Speak() => "...";
}

class Dog : Animal
{
    public override string Speak() => "woof";
}

class Program
{
    static void Main()
    {
        Animal animal = new Dog();
        if (animal.Speak() != "woof")
        {
            throw new System.Exception("The override should run.");
        }
    }
}
