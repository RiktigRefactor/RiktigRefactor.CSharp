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
        var sum = 0;
        foreach (var item in new Box())
        {
            sum += item;
        }

        if (sum != 1)
        {
            throw new System.Exception("foreach should use the extension GetEnumerator.");
        }
    }
}
