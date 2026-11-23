using Lib;

if (new Base().Call() != "object")
{
    throw new System.Exception("wrong");
}

if (Helper.Run() != "object")
{
    throw new System.Exception("wrong");
}

static class Helper
{
    public static string Run() => new Base().Pick(2);
}
