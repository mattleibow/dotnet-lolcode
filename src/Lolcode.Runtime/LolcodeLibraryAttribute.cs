namespace Lolcode.Runtime;

/// <summary>
/// Marks a CLR type as an explicitly importable LOLCODE library and specifies its
/// <c>CAN HAS</c> library name.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class LolcodeLibraryAttribute(string name) : Attribute
{
    /// <summary>Gets the LOLCODE library name.</summary>
    public string Name { get; } = name;
}
