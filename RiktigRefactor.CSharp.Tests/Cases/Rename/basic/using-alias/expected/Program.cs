using Numbers = System.Collections.Generic.List<int>;

class Program
{
    static void Main()
    {
        Numbers list = new Numbers();
        list.Add(1);
        if (list.Count != 1)
        {
            throw new System.Exception("The list should have one item.");
        }
    }
}
