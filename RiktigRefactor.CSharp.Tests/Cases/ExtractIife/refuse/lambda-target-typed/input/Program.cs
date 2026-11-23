class Program
{
    static int Run(System.Func<int> make) => make();

    static void Main()
    {
        if (Run(/*[*/() => 42/*]*/) != 42)
        {
            throw new System.Exception("Run should return 42.");
        }
    }
}
