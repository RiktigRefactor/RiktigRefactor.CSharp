static class IntExtensions
{
    extension(int value)
    {
        public int Twice() => value * 2;
    }
}

class Program
{
    static void Main()
    {
        if (3.Twice() != 6)
        {
            throw new System.Exception("Twice should double the receiver.");
        }
    }
}
