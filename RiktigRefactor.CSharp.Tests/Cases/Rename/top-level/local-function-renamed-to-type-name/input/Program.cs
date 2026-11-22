int /*|*/Two() => 2;
if (Two() != 2 || C.V != 1)
{
    throw new System.Exception("Both the local function and the type should be reachable.");
}

static class C
{
    public static int V = 1;
}
