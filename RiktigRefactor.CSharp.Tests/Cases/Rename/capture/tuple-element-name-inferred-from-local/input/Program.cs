class Program
{
    static void Main()
    {
        var /*|*/count = 1;
        var pair = (count, 2);
        if (pair.count != 1)
        {
            throw new System.Exception("The tuple element should be named after the local.");
        }
    }
}
