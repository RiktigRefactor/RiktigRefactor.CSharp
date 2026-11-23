class Animal
{
    public virtual string Sound() => "...";
}

class Dog : Animal
{
    public override string Sound() => /*[*/base.Sound() + "woof"/*]*/;
}

class Program
{
    static void Main()
    {
        if (new Dog().Sound() != "...woof")
        {
            throw new System.Exception("The dog should say ...woof.");
        }
    }
}
