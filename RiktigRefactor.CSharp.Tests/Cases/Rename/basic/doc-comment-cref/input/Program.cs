class Program
{
    /// <summary>Calls <see cref="Helper"/> and returns its result.</summary>
    static int Run() => Helper();

    /// <summary>Returns the answer.</summary>
    static int /*|*/Helper() => 42;

    static void Main()
    {
        if (Run() != 42)
        {
            throw new System.Exception("Run should return 42.");
        }
    }
}
