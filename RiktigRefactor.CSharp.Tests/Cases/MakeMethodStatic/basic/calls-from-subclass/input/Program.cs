using System;

class Animal
{
    protected string name = "animal";

    public string /*|*/Greet(string greeting) => greeting + ", " + name;
}

class Dog : Animal
{
    public Dog() => name = "dog";

    public string Twice() => base.Greet("hi") + "; " + Greet("hello") + "; " + this.Greet("hey");
}

class Program
{
    static void Main()
    {
        var dog = new Dog();
        var text = dog.Twice() + "; " + dog.Greet("yo");
        if (text != "hi, dog; hello, dog; hey, dog; yo, dog")
        {
            throw new Exception(text);
        }
    }
}
