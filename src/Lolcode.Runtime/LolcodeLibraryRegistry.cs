namespace Lolcode.Runtime;

internal sealed record LolcodeLibraryDefinition(string AssemblyName, string TypeName);

internal sealed class LolcodeLibraryRegistry
{
    private readonly Dictionary<string, LolcodeLibraryDefinition> _definitions =
        new(StringComparer.Ordinal);

    internal void Register(string name, string assemblyName, string typeName)
    {
        var definition = new LolcodeLibraryDefinition(assemblyName, typeName);
        if (_definitions.TryGetValue(name, out LolcodeLibraryDefinition? existing))
        {
            if (existing == definition)
                return;
            throw new LolRuntimeException($"Ambiguous LOLCODE library: {name}");
        }
        _definitions.Add(name, definition);
    }

    internal bool TryGet(string name, out LolcodeLibraryDefinition definition) =>
        _definitions.TryGetValue(name, out definition!);
}
