int Total = 1;
int Twice() => Total * 2;
if (Twice() != 2)
{
    throw new System.Exception("Twice should double the top-level local.");
}
