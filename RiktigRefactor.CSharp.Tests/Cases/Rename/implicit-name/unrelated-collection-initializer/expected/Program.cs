using System.Collections.Generic;

class Inventory
{
    public int Count;

    public void Add(int amount) => this.Count += amount;
}

class Program
{
    static void Main()
    {
        var numbers = new List<int> { 1, 2 };
        var inventory = new Inventory();
        inventory.Add(3);
        if (numbers.Count != 2 || inventory.Count != 3)
        {
            throw new System.Exception("The list should hold 2 numbers and the inventory 3 items.");
        }
    }
}
