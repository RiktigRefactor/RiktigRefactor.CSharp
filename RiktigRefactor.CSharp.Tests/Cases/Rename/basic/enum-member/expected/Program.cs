enum Color
{
    Red,
    Lime,
    Blue,
}

class Program
{
    static void Main()
    {
        var color = Color.Lime;
        var result = color switch
        {
            Color.Lime => 1,
            _ => 0,
        };
        if (result != 1 || (int)color != 1)
        {
            throw new System.Exception("The enum member should still select the first switch arm.");
        }
    }
}
