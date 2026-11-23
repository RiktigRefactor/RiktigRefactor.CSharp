using System;

class Animal
{
    protected string name = "animal";

    public static string Greet(Animal animal, string greeting) => greeting + ", " + animal.name;
}

class Dog : Animal
{
    public Dog() => name = "dog";

    public string Twice() => Animal.Greet(this, "hi") + "; " + Animal.Greet(this, "hello") + "; " + Animal.Greet(this, "hey");
}

class Program
{
    static void Main()
    {
        var dog = new Dog();
        var text = dog.Twice() + "; " + Animal.Greet(dog, "yo");
        if (text != "hi, dog; hello, dog; hey, dog; yo, dog")
        {
            throw new Exception(text);
        }
    }
}
