using System.Collections;
using System.Collections.Generic;

class Base : IEnumerable
{
    public List<int> Items = new();

    public void Add(object value) => this.Items.Add(-1);

    public IEnumerator GetEnumerator() => this.Items.GetEnumerator();
}

class Bag : Base
{
    public void /*|*/Put(int value) => this.Items.Add(value * 10);
}

class Program
{
    static void Main()
    {
        var bag = new Bag { 1 };
        if (bag.Items[0] != -1)
        {
            throw new System.Exception($"The initializer should call Base.Add(object), but stored {bag.Items[0]}.");
        }
    }
}
