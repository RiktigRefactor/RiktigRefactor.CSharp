class Resource : System.IDisposable
{
    public bool Disposed;

    public void /*|*/Dispose() => Disposed = true;
}

class Program
{
    static void Main()
    {
        var resource = new Resource();
        using (resource)
        {
        }

        if (!resource.Disposed)
        {
            throw new System.Exception("using should call Dispose.");
        }
    }
}
