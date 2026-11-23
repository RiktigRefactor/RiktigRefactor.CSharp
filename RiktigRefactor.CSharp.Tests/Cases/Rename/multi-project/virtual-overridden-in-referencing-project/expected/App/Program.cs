using Lib;

Base value = new Derived();
if (value.Explain() != "derived")
{
    throw new System.Exception("wrong");
}

class Derived : Base
{
    public override string Explain() => "derived";
}
