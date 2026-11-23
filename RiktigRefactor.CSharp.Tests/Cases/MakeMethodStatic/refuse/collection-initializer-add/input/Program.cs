using System.Collections;
using System.Collections.Generic;

class Bag : IEnumerable<int>
{
    readonly List<int> items = new List<int>();

    public void /*|*/Add(int item) => items.Add(item);

    public IEnumerator<int> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

class Program
{
    static void Main()
    {
        var bag = new Bag { 1, 2 };
        var count = 0;
        foreach (var item in bag)
        {
            count += item;
        }

        if (count != 3)
        {
            throw new System.Exception("Expected 3.");
        }
    }
}
