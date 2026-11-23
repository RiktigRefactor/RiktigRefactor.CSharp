class Box<T>
{
    public static int /*|*/Count;
}

class Counters
{
}

class Program
{
    static void Main()
    {
        Box<int>.Count++;
        Box<string>.Count += 2;
        if (Box<int>.Count != 1 || Box<string>.Count != 2)
        {
            throw new System.Exception("Each constructed type has its own field.");
        }
    }
}
