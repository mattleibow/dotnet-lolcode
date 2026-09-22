using System.Reflection;
using Lolcode.Runtime;

namespace Lolcode.CodeAnalysis;

internal sealed record LolcodeLibraryProviderDeclaration(
    string LolName,
    string AssemblyName,
    string ExportTypeName,
    bool IsBuiltIn,
    int ContractVersion);

internal sealed record ProviderDiscoveryResult(
    IReadOnlyList<LolcodeLibraryProviderDeclaration> Providers,
    IReadOnlyList<string> Errors);

/// <summary>
/// Reads provider declarations from PE metadata only. The compiler must never
/// execute target assemblies while processing a project reference.
/// </summary>
internal static class ProviderDiscovery
{
    private const string ProviderAttributeName =
        "Lolcode.Runtime.LolcodeLibraryProviderAttribute";

    private static readonly IReadOnlyDictionary<string, LolcodeLibraryProviderDeclaration>
        BuiltIns = new Dictionary<string, LolcodeLibraryProviderDeclaration>(StringComparer.Ordinal)
        {
            ["STRING"] = new(
                "STRING",
                "Lolcode.Runtime.String",
                "Lolcode.Runtime.String.StringLibrary",
                true,
                LolcodeLibraryProviderAttribute.CurrentContractVersion),
            ["STDLIB"] = new(
                "STDLIB",
                "Lolcode.Runtime.Stdlib",
                "Lolcode.Runtime.Stdlib.StdlibLibrary",
                true,
                LolcodeLibraryProviderAttribute.CurrentContractVersion),
            ["STDIO"] = new(
                "STDIO",
                "Lolcode.Runtime.Stdio",
                "Lolcode.Runtime.Stdio.StdioLibrary",
                true,
                LolcodeLibraryProviderAttribute.CurrentContractVersion),
            ["SOCKS"] = new(
                "SOCKS",
                "Lolcode.Runtime.Socks",
                "Lolcode.Runtime.Socks.SocksLibrary",
                true,
                LolcodeLibraryProviderAttribute.CurrentContractVersion),
        };

    internal static ProviderDiscoveryResult Discover(
        IEnumerable<string>? referenceAssemblyPaths,
        string? runtimeAssemblyPath = null)
    {
        string[] candidates = referenceAssemblyPaths?
            .Where(File.Exists)
            .AppendIfNotNull(runtimeAssemblyPath)
            .AppendIfNotNull(typeof(LolcodeLibraryProviderAttribute).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()
            ?? [];
        if (candidates.Length == 0)
            return new ProviderDiscoveryResult([], []);
        string[] resolverPaths = candidates
            .Concat(GetTrustedPlatformAssemblyPaths())
            .GroupBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();

        var errors = new List<string>();
        var providers = new List<LolcodeLibraryProviderDeclaration>();
        try
        {
            foreach (string path in candidates)
            {
                if (string.Equals(
                        Path.GetFileNameWithoutExtension(path),
                        "System.Runtime",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                using var context = new MetadataLoadContext(
                    new PathAssemblyResolver(resolverPaths),
                    coreAssemblyName: "System.Runtime");
                Assembly assembly;
                try
                {
                    assembly = context.LoadFromAssemblyPath(path);
                }
                catch (Exception ex) when (ex is BadImageFormatException or FileLoadException or
                    FileNotFoundException or ArgumentException)
                {
                    errors.Add($"Could not read provider metadata from '{path}': {ex.Message}");
                    continue;
                }

                IList<CustomAttributeData> attributes;
                try
                {
                    attributes = assembly.GetCustomAttributesData();
                }
                catch (Exception ex) when (ex is FileLoadException or TypeLoadException or
                    BadImageFormatException)
                {
                    errors.Add($"Could not read provider attributes from '{path}': {ex.Message}");
                    continue;
                }

                foreach (CustomAttributeData attribute in attributes.Where(attribute =>
                    string.Equals(
                        attribute.AttributeType.FullName,
                        ProviderAttributeName,
                        StringComparison.Ordinal)))
                {
                    if (!string.Equals(
                            attribute.AttributeType.Assembly.GetName().Name,
                            typeof(LolcodeLibraryProviderAttribute).Assembly.GetName().Name,
                            StringComparison.Ordinal))
                    {
                        errors.Add(
                            $"Provider declaration in '{path}' uses a spoofed {ProviderAttributeName}.");
                        continue;
                    }

                    if (!TryReadDeclaration(attribute, assembly, out var provider, out string? error))
                    {
                        errors.Add($"Invalid LOLCODE provider declaration in '{path}': {error}");
                        continue;
                    }

                    providers.Add(provider!);
                }
            }
        }
        catch (Exception ex) when (ex is FileLoadException or FileNotFoundException or
            TypeLoadException or BadImageFormatException or ArgumentException)
        {
            errors.Add($"Could not initialize metadata-only provider discovery: {ex.Message}");
        }

        foreach (IGrouping<string, LolcodeLibraryProviderDeclaration> duplicate in providers.GroupBy(
            provider => provider.LolName,
            StringComparer.Ordinal))
        {
            if (duplicate.Take(2).Count() > 1)
                errors.Add($"Duplicate LOLCODE provider name '{duplicate.Key}' was declared by referenced assemblies.");
        }

        foreach (LolcodeLibraryProviderDeclaration provider in providers)
        {
            if (provider.ContractVersion != LolcodeLibraryProviderAttribute.CurrentContractVersion)
            {
                errors.Add(
                    $"Provider '{provider.LolName}' uses unsupported contract version {provider.ContractVersion}.");
            }

            if (BuiltIns.TryGetValue(provider.LolName, out var builtIn))
            {
                if (provider != builtIn)
                {
                    errors.Add(
                        $"Provider '{provider.LolName}' attempts to replace the reserved built-in provider.");
                }
            }
            else if (provider.IsBuiltIn)
            {
                errors.Add(
                    $"Provider '{provider.LolName}' cannot declare itself as a reserved built-in provider.");
            }
        }

        return new ProviderDiscoveryResult(
            providers
                .OrderBy(provider => provider.LolName, StringComparer.Ordinal)
                .ThenBy(provider => provider.AssemblyName, StringComparer.Ordinal)
                .ToArray(),
            errors.Distinct(StringComparer.Ordinal).ToArray());
    }

    private static bool TryReadDeclaration(
        CustomAttributeData attribute,
        Assembly declaringAssembly,
        out LolcodeLibraryProviderDeclaration? provider,
        out string? error)
    {
        provider = null;
        error = null;
        if (attribute.ConstructorArguments.Count != 4)
        {
            error = "the attribute constructor must contain name, provider type, built-in status, and contract version";
            return false;
        }

        object? name = attribute.ConstructorArguments[0].Value;
        object? typeValue = attribute.ConstructorArguments[1].Value;
        object? isBuiltIn = attribute.ConstructorArguments[2].Value;
        object? version = attribute.ConstructorArguments[3].Value;
        if (name is not string lolName || string.IsNullOrWhiteSpace(lolName) ||
            typeValue is not Type providerType || string.IsNullOrWhiteSpace(providerType.FullName) ||
            isBuiltIn is not bool builtIn || version is not int contractVersion)
        {
            error = "the attribute has invalid constructor arguments";
            return false;
        }

        if (providerType.Assembly != declaringAssembly ||
            declaringAssembly.GetType(providerType.FullName, throwOnError: false) is null)
        {
            error = $"the export type '{providerType.FullName}' must be defined by the declaring assembly";
            return false;
        }

        string? assemblyName = declaringAssembly.GetName().Name;
        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            error = "the declaring assembly has no simple assembly name";
            return false;
        }

        provider = new LolcodeLibraryProviderDeclaration(
            lolName,
            assemblyName,
            providerType.FullName,
            builtIn,
            contractVersion);
        return true;
    }

    private static IEnumerable<string> AppendIfNotNull(this IEnumerable<string> values, string? value) =>
        string.IsNullOrWhiteSpace(value) ? values : values.Append(value);

    private static IEnumerable<string> GetTrustedPlatformAssemblyPaths() =>
        AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string paths
            ? paths.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            : [];
}
