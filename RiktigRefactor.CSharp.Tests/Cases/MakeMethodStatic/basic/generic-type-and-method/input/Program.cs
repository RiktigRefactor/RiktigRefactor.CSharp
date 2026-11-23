using System;

class Box<T>
{
    readonly T value;

    public Box(T value) => this.value = value;

    public U /*|*/Map<U>(Func<T, U> map) => map(value);
}

class Program
{
    static void Main()
    {
        var box = new Box<int>(20);
        var text = box.Map(number => (number + 1).ToString());
        var length = box.Map<long>(number => number * 2);
        if (text != "21" || length != 40)
        {
            throw new Exception($"{text} {length}");
        }
    }
}
