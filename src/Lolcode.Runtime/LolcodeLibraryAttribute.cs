namespace Lolcode.Runtime;

/// <summary>
/// Marks a top-level public static CLR type as the export container for a
/// <c>CAN HAS</c> library.
/// </summary>
/// <remarks>
/// This attribute does not create CLR global methods or hide the marked type from
/// C# or other .NET languages.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class LolcodeLibraryAttribute : Attribute
{
}
