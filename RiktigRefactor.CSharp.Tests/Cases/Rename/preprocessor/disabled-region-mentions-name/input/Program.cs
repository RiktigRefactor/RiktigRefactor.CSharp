class Program
{
    static void Main()
    {
        int /*|*/count = 0;
        count++;
#if NEVER_DEFINED
        count--;
#endif
        if (count != 1)
        {
            throw new System.Exception("count should be 1.");
        }
    }
}
