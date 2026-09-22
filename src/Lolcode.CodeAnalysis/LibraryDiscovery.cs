using System.Collections.Immutable;
using System.Reflection;
using Lolcode.CodeAnalysis.Errors;
using Lolcode.CodeAnalysis.Text;

namespace Lolcode.CodeAnalysis;

internal sealed record LibraryDefinition(string Name, string AssemblyName, string TypeName);

internal sealed record LibraryDiscoveryResult(
    ImmutableArray<LibraryDefinition> Definitions,
    ImmutableArray<Diagnostic> Diagnostics);

/// <summary>Reads explicitly opted-in library metadata without executing referenced code.</summary>
internal static class LibraryDiscovery
{
    private const string AttributeName = "Lolcode.Runtime.LolcodeLibraryAttribute";

    internal static LibraryDiscoveryResult Discover(
        IEnumerable<string>? referenceAssemblyPaths,
        string? runtimeAssemblyPath)
    {
        string[] references = (referenceAssemblyPaths ?? [])
            .Append(runtimeAssemblyPath)
            .Where(static path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            .Select(static path => path!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
        if (references.Length == 0)
            return new([], []);

        string[] resolverPaths = references.Append(runtimeAssemblyPath)
            .Where(static path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            .Select(static path => path!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var definitions = ImmutableArray.CreateBuilder<LibraryDefinition>();
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var names = new Dictionary<string, LibraryDefinition>(StringComparer.Ordinal);

        try
        {
            using var context = new MetadataLoadContext(
                new PathAssemblyResolver(resolverPaths),
                coreAssemblyName: "System.Runtime");
            foreach (string path in references.OrderBy(static path => path, StringComparer.Ordinal))
            {
                Assembly assembly;
                try { assembly = context.LoadFromAssemblyPath(path); }
                catch (Exception ex) when (ex is BadImageFormatException or FileLoadException or FileNotFoundException or TypeLoadException or ArgumentException)
                {
                    diagnostics.Add(Error(path, $"Unable to read library metadata: {ex.Message}"));
                    continue;
                }

                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (Exception ex) when (ex is ReflectionTypeLoadException or FileLoadException or TypeLoadException)
                {
                    diagnostics.Add(Error(path, $"Unable to read library types: {ex.Message}"));
                    continue;
                }

                foreach (Type type in types.OrderBy(static type => type.FullName, StringComparer.Ordinal))
                {
                    CustomAttributeData? attribute;
                    try
                    {
                        attribute = type.GetCustomAttributesData().SingleOrDefault(IsLibraryAttribute);
                    }
                    catch (Exception ex) when (ex is CustomAttributeFormatException or TypeLoadException)
                    {
                        diagnostics.Add(Error(path, $"Unable to read library attribute: {ex.Message}"));
                        continue;
                    }
                    if (attribute is null)
                        continue;
                    if (!TryReadName(attribute, out string? name))
                    {
                        diagnostics.Add(Error(path, $"Library type '{type.FullName}' has a malformed LolcodeLibraryAttribute."));
                        continue;
                    }
                    if (!IsValidIdentifier(name!))
                    {
                        diagnostics.Add(Error(path, $"LOLCODE library name '{name}' is not a valid identifier."));
                        continue;
                    }
                    if (!IsValidLibraryType(type, out string? reason))
                    {
                        diagnostics.Add(Error(path, $"LOLCODE library '{name}' has invalid type '{type.FullName}': {reason}"));
                        continue;
                    }

                    var definition = new LibraryDefinition(name!, assembly.GetName().Name!, type.FullName!);
                    if (names.TryGetValue(name!, out LibraryDefinition? prior))
                    {
                        diagnostics.Add(Error(path, $"LOLCODE library name '{name}' is declared by both '{prior.AssemblyName}:{prior.TypeName}' and '{definition.AssemblyName}:{definition.TypeName}'."));
                        continue;
                    }
                    names.Add(name!, definition);
                    definitions.Add(definition);
                }
            }
        }
        catch (Exception ex) when (ex is FileLoadException or FileNotFoundException or InvalidOperationException or ArgumentException)
        {
            diagnostics.Add(Error(runtimeAssemblyPath ?? string.Empty, $"Unable to initialize library discovery: {ex.Message}"));
        }

        return new(definitions.ToImmutable(), diagnostics.ToImmutable());
    }

    private static bool IsLibraryAttribute(CustomAttributeData attribute) =>
        attribute.AttributeType.FullName == AttributeName &&
        attribute.AttributeType.Assembly.GetName().Name == "Lolcode.Runtime";

    private static bool TryReadName(CustomAttributeData attribute, out string? name)
    {
        name = null;
        if (attribute.ConstructorArguments is not [{ ArgumentType.FullName: "System.String", Value: string value }])
            return false;
        name = value;
        return true;
    }

    private static bool IsValidLibraryType(Type type, out string? reason)
    {
        reason = null;
        if (!type.IsPublic || type.IsNested || !type.IsClass || type.IsAbstract || !type.IsSealed || type.ContainsGenericParameters)
        {
            reason = "the type must be a public, non-nested, sealed, concrete, non-generic class";
            return false;
        }
        if (type.GetConstructor(Type.EmptyTypes) is null)
        {
            reason = "the type must have an accessible public parameterless constructor";
            return false;
        }
        MethodInfo[] eligible = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(static method => !method.IsSpecialName && method.Name != nameof(IDisposable.Dispose))
            .ToArray();
        if (eligible.Length == 0)
        {
            reason = "the type has no eligible public instance functions";
            return false;
        }
        if (eligible.GroupBy(static method => method.Name, StringComparer.Ordinal).Any(static group => group.Skip(1).Any()))
        {
            reason = "the type has ambiguous overloaded functions";
            return false;
        }
        if (eligible.Any(static method => !IsEligibleSignature(method)))
        {
            reason = "the type has unsupported function signatures";
            return false;
        }
        return true;
    }

    private static bool IsEligibleSignature(MethodInfo method) =>
        !method.IsGenericMethodDefinition && !method.ContainsGenericParameters &&
        IsBoundaryType(method.ReturnType, allowVoid: true) &&
        method.GetParameters().All(parameter =>
            !parameter.IsOut && !parameter.ParameterType.IsByRef &&
            !parameter.ParameterType.IsPointer && !parameter.ParameterType.IsByRefLike &&
            IsBoundaryType(parameter.ParameterType, allowVoid: false));

    private static bool IsBoundaryType(Type type, bool allowVoid) =>
        (allowVoid && type.FullName == "System.Void") ||
        type.FullName is "System.Object" or "System.String" or "System.Int32" or "System.Double" or "System.Boolean";

    private static bool IsValidIdentifier(string value) =>
        value.Length > 0 && char.IsLetter(value[0]) &&
        value.All(static character => char.IsLetterOrDigit(character) || character == '_');

    private static Diagnostic Error(string path, string message) =>
        Diagnostic.Create(DiagnosticDescriptors.InvalidLibraryProvider,
            new TextLocation(path, default, 0, 0, 0, 0), message);
}
