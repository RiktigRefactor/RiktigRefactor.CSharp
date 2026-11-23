public class Node
{
    public int Value = 2;
}

public static class Nodes
{
    public static int /*|*/Repeat(Node node, int times) => times == 0 ? 0 : node.Value + Repeat(node, times - 1);
}

class Program
{
    static void Main()
    {
        var node = new Node();
        if (Nodes.Repeat(node, 3) != 6)
        {
            throw new System.Exception("Expected 6.");
        }
    }
}
