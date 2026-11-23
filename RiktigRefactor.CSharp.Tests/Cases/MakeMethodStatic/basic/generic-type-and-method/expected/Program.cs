using System;

class Box<T>
{
    readonly T value;

    public Box(T value) => this.value = value;

    public static U Map<U>(Box<T> box, Func<T, U> map) => map(box.value);
}

class Program
{
    static void Main()
    {
        var box = new Box<int>(20);
        var text = Box<int>.Map(box, number => (number + 1).ToString());
        var length = Box<int>.Map<long>(box, number => number * 2);
        if (text != "21" || length != 40)
        {
            throw new Exception($"{text} {length}");
        }
    }
}
