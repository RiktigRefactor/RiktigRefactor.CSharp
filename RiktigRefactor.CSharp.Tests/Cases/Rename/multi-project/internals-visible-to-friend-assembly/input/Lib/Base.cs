[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("App")]

namespace Lib;

public class Base
{
    public string Call() => Pick(1);

    public string Pick(object value) => "object";

    internal string /*|*/Other(int value) => "int";
}
