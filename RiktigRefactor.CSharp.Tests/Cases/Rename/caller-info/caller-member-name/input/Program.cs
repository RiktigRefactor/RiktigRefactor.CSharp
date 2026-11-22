using System.Reflection;
using System.Runtime.CompilerServices;

class Program
{
    static string Who([CallerMemberName] string name = "") => name;

    static string /*|*/Caller() => Who();

    static void Main()
    {
        var method = typeof(Program).GetMethod(Caller(), BindingFlags.NonPublic | BindingFlags.Static);
        if (method is null)
        {
            throw new System.Exception("The caller name should name an existing method.");
        }
    }
}
