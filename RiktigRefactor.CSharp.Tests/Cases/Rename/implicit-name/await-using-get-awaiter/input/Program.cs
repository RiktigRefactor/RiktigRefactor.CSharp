using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class Log
{
    public static string Value = "";
}

class Pending
{
    public TaskAwaiter /*|*/Other()
    {
        Log.Value = "instance";
        return Task.CompletedTask.GetAwaiter();
    }
}

static class PendingExtensions
{
    public static TaskAwaiter GetAwaiter(this Pending pending)
    {
        Log.Value = "extension";
        return Task.CompletedTask.GetAwaiter();
    }
}

class Resource
{
    public Pending DisposeAsync() => new Pending();
}

class Program
{
    static async Task Main()
    {
        await using (new Resource())
        {
        }

        if (Log.Value != "extension")
        {
            throw new System.Exception("await using should use the extension GetAwaiter.");
        }
    }
}
