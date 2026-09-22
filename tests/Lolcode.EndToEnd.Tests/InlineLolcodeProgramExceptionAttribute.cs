namespace Lolcode.EndToEnd.Tests;

[AttributeUsage(AttributeTargets.Method)]
internal sealed class InlineLolcodeProgramExceptionAttribute(string reason) : Attribute
{
    public string Reason { get; } = reason;
}
