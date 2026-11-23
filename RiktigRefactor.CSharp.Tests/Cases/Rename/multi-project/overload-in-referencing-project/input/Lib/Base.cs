namespace Lib;

public class Base
{
    public string Call() => Pick(1);

    public string Pick(object value) => "object";

    public string /*|*/Other(int value) => "int";
}
