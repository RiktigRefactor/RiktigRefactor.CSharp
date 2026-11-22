using /*|*/L = System.Collections.Generic.List<int>;

class Program
{
    static void Main()
    {
        L list = new L();
        list.Add(1);
        if (list.Count != 1)
        {
            throw new System.Exception("The list should have one item.");
        }
    }
}
