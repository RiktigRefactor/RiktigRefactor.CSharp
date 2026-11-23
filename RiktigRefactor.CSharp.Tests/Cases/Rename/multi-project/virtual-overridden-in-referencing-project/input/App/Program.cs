using Lib;

Base value = new Derived();
if (value.Describe() != "derived")
{
    throw new System.Exception("wrong");
}

class Derived : Base
{
    public override string Describe() => "derived";
}
