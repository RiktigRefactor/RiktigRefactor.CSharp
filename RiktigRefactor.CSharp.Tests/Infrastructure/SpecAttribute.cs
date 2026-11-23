namespace RiktigRefactor.CSharp.Tests.Infrastructure;

/// <summary>A language-specification section a test covers, cited as the test format describes.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class SpecAttribute(string citation) : Attribute
{
    public string Citation { get; } = citation;
}
