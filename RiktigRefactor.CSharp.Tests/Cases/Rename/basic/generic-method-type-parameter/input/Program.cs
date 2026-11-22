class Program
{
    static T First</*|*/T>(T[] items) => items[0];

    static void Main()
    {
        if (First(new[] { 5, 6 }) != 5)
        {
            throw new System.Exception("First should return the first item.");
        }
    }
}
