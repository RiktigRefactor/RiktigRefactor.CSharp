using Lib;

if (new Derived().Pick() != "derived")
{
    throw new System.Exception("wrong");
}

if (new Derived().Other() != "base")
{
    throw new System.Exception("wrong");
}

class Derived : Base
{
    public string Pick() => "derived";
}
