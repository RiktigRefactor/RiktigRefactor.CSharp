using System.Collections.Generic;

class Box
{
    public IEnumerator<int> /*|*/Other() { yield return 2; }
}

static class BoxExtensions
{
    public static IEnumerator<int> GetEnumerator(this Box box) { yield return 1; }
}

class Program
{
    static void Main()
    {
        int[] items = [.. new Box()];
        if (items[0] != 1)
        {
            throw new System.Exception("The spread should use the extension GetEnumerator.");
        }
    }
}
