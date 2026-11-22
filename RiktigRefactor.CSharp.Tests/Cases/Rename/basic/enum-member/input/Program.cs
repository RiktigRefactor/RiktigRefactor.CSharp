enum Color
{
    Red,
    /*|*/Green,
    Blue,
}

class Program
{
    static void Main()
    {
        var color = Color.Green;
        var result = color switch
        {
            Color.Green => 1,
            _ => 0,
        };
        if (result != 1 || (int)color != 1)
        {
            throw new System.Exception("The enum member should still select the first switch arm.");
        }
    }
}
