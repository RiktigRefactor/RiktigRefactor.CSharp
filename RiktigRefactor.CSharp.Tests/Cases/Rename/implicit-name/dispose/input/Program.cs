class Log
{
    public static int Disposed;
}

ref struct Resource
{
    public void /*|*/Dispose() => Log.Disposed++;
}

class Program
{
    static void Main()
    {
        using (new Resource())
        {
        }

        if (Log.Disposed != 1)
        {
            throw new System.Exception("using should call Dispose.");
        }
    }
}
