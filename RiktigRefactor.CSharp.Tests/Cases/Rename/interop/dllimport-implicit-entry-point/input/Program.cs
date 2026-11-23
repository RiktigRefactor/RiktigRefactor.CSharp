using System;
using System.Reflection;
using System.Runtime.InteropServices;

class Program
{
    [DllImport("c", CharSet = CharSet.Ansi)]
    static extern nint /*|*/strlen(string text);

    static void Main()
    {
        NativeLibrary.SetDllImportResolver(typeof(Program).Assembly, LoadCRuntime);
        if (strlen("abc") != 3)
        {
            throw new Exception("The C runtime should measure three characters.");
        }
    }

    static nint LoadCRuntime(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (OperatingSystem.IsWindows())
        {
            return NativeLibrary.Load("msvcrt");
        }

        if (OperatingSystem.IsMacOS())
        {
            return NativeLibrary.Load("libSystem.dylib");
        }

        return NativeLibrary.Load("libc.so.6");
    }
}
