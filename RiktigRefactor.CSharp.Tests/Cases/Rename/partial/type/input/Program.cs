class Program
{
    static void Main()
    {
        if (new Widget().Run() != 42)
        {
            throw new System.Exception("Run should return 42.");
        }
    }
}
