namespace Lolcode.Runtime;

/// <summary>Maps a friendly <c>CAN HAS</c> name to an explicit module export type.</summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class LolcodeModuleAttribute(string name, Type exportType) : Attribute
{
    /// <summary>Gets the friendly LOLCODE import name.</summary>
    public string Name { get; } = name;

    /// <summary>Gets the public static export type.</summary>
    public Type ExportType { get; } = exportType;
}
