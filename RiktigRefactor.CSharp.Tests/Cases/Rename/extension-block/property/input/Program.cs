static class IntExtensions
{
    extension(int number)
    {
        public int /*|*/Twice => number * 2;
    }
}

class Program
{
    static void Main()
    {
        if (3.Twice != 6)
        {
            throw new System.Exception("Twice should double the receiver.");
        }
    }
}
