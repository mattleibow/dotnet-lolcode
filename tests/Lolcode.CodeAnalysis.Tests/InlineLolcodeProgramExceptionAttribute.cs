namespace Lolcode.CodeAnalysis.Tests;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
internal sealed class InlineLolcodeProgramExceptionAttribute(string reason) : Attribute
{
    internal string Reason { get; } = reason;
}
