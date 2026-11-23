public class Node
{
    public int Value = 2;

    public int Repeat(int times) => times == 0 ? 0 : this.Value + this.Repeat(times - 1);
}

public static class Nodes
{
}

class Program
{
    static void Main()
    {
        var node = new Node();
        if (node.Repeat(3) != 6)
        {
            throw new System.Exception("Expected 6.");
        }
    }
}
