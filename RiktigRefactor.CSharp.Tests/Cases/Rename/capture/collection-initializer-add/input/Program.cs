using System.Collections;
using System.Collections.Generic;

class Bag : IEnumerable
{
    public List<string> Log { get; } = new();

    public void Add(object item) => Log.Add("object");

    public void /*|*/Insert(int item) => Log.Add("int");

    public IEnumerator GetEnumerator() => Log.GetEnumerator();
}

class Program
{
    static void Main()
    {
        var bag = new Bag { 1 };
        bag.Insert(2);
        if (string.Join(",", bag.Log) != "object,int")
        {
            throw new System.Exception("The initializer should call Add(object).");
        }
    }
}
