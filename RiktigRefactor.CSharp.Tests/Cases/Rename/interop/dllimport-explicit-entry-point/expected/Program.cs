using System;
using System.Reflection;
using System.Runtime.InteropServices;

class Program
{
    [DllImport("c", EntryPoint = "strlen")]
    static extern nint MeasureText(string text);

    static void Main()
    {
        NativeLibrary.SetDllImportResolver(typeof(Program).Assembly, LoadCRuntime);
        if (MeasureText("abc") != 3)
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
