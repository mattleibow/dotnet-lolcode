using System.Collections.Immutable;
using System.Reflection;
using Lolcode.CodeAnalysis.Errors;
using Lolcode.CodeAnalysis.Text;

namespace Lolcode.CodeAnalysis;

internal sealed record ModuleAlias(string Name, string AssemblyName, string TypeName);

internal sealed record ModuleAliasDiscoveryResult(
    ImmutableArray<ModuleAlias> Aliases,
    ImmutableArray<Diagnostic> Diagnostics);

internal static class ModuleAliasDiscovery
{
    private const string ModuleAttributeName = "Lolcode.Runtime.LolcodeModuleAttribute";
    private const string LibraryAttributeName = "Lolcode.Runtime.LolcodeLibraryAttribute";
    private static readonly HashSet<string> ReservedNames =
        new(["STRING", "STDLIB", "STDIO", "SOCKS"], StringComparer.Ordinal);

    internal static ModuleAliasDiscoveryResult Discover(
        IEnumerable<string>? referenceAssemblyPaths,
        string? runtimeAssemblyPath)
    {
        string[] references = referenceAssemblyPaths?
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
        if (references.Length == 0)
            return new([], []);

        string[] resolverPaths = references
            .Append(runtimeAssemblyPath)
            .Where(static path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            .Select(static path => path!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var aliases = ImmutableArray.CreateBuilder<ModuleAlias>();
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var declarations = new Dictionary<string, ModuleAlias>(StringComparer.Ordinal);

        try
        {
            if (!resolverPaths.Any(path =>
                string.Equals(
                    Path.GetFileName(path),
                    "System.Runtime.dll",
                    StringComparison.OrdinalIgnoreCase)))
            {
                return new([], [CreateDiagnostic(
                    runtimeAssemblyPath ?? string.Empty,
                    "The target reference assemblies do not contain System.Runtime.dll.")]);
            }

            using var context = new MetadataLoadContext(
                new PathAssemblyResolver(resolverPaths),
                coreAssemblyName: "System.Runtime");
            foreach (string path in references)
            {
                Assembly assembly;
                try
                {
                    assembly = context.LoadFromAssemblyPath(path);
                }
                catch (Exception ex) when (
                    ex is BadImageFormatException or FileLoadException or FileNotFoundException or
                    TypeLoadException or ArgumentException)
                {
                    diagnostics.Add(CreateDiagnostic(path, $"Unable to read module aliases: {ex.Message}"));
                    continue;
                }

                IList<CustomAttributeData> attributes;
                try
                {
                    attributes = assembly.GetCustomAttributesData();
                }
                catch (Exception ex) when (
                    ex is CustomAttributeFormatException or FileLoadException or TypeLoadException)
                {
                    diagnostics.Add(CreateDiagnostic(path, $"Unable to read module aliases: {ex.Message}"));
                    continue;
                }

                foreach (CustomAttributeData attribute in attributes.Where(IsModuleAttribute))
                {
                    if (!TryReadAlias(attribute, out string? name, out Type? exportType, out string? error))
                    {
                        diagnostics.Add(CreateDiagnostic(path, error!));
                        continue;
                    }

                    if (!IsValidIdentifier(name!))
                    {
                        diagnostics.Add(CreateDiagnostic(
                            path,
                            $"LOLCODE module alias '{name}' is not a valid identifier."));
                        continue;
                    }
                    if (ReservedNames.Contains(name!))
                    {
                        diagnostics.Add(CreateDiagnostic(
                            path,
                            $"LOLCODE module alias '{name}' is reserved for a built-in library."));
                        continue;
                    }
                    if (exportType!.Assembly != assembly || exportType.FullName is null)
                    {
                        diagnostics.Add(CreateDiagnostic(
                            path,
                            $"LOLCODE module alias '{name}' must target a type declared in '{assembly.GetName().Name}'."));
                        continue;
                    }
                    if (!IsValidExportType(exportType, out string? typeError))
                    {
                        diagnostics.Add(CreateDiagnostic(
                            path,
                            $"LOLCODE module alias '{name}' has invalid export type '{exportType.FullName}': {typeError}"));
                        continue;
                    }

                    var alias = new ModuleAlias(name!, assembly.GetName().Name!, exportType.FullName);
                    if (declarations.TryGetValue(name!, out ModuleAlias? existing))
                    {
                        diagnostics.Add(CreateDiagnostic(
                            path,
                            $"LOLCODE module alias '{name}' is declared by both " +
                            $"'{existing.AssemblyName}:{existing.TypeName}' and " +
                            $"'{alias.AssemblyName}:{alias.TypeName}'."));
                        continue;
                    }

                    declarations.Add(name!, alias);
                    aliases.Add(alias);
                }
            }
        }
        catch (Exception ex) when (
            ex is FileLoadException or FileNotFoundException or InvalidOperationException or ArgumentException)
        {
            diagnostics.Add(CreateDiagnostic(
                runtimeAssemblyPath ?? string.Empty,
                $"Unable to initialize module alias discovery: {ex.Message}"));
        }

        return new(aliases.ToImmutable(), diagnostics.ToImmutable());
    }

    private static bool IsModuleAttribute(CustomAttributeData attribute) =>
        attribute.AttributeType.FullName == ModuleAttributeName &&
        attribute.AttributeType.Assembly.GetName().Name == "Lolcode.Runtime";

    private static bool TryReadAlias(
        CustomAttributeData attribute,
        out string? name,
        out Type? exportType,
        out string? error)
    {
        name = null;
        exportType = null;
        error = null;
        if (attribute.ConstructorArguments.Count != 2 ||
            attribute.ConstructorArguments[0].ArgumentType.FullName != "System.String" ||
            attribute.ConstructorArguments[0].Value is not string alias ||
            attribute.ConstructorArguments[1].ArgumentType.FullName != "System.Type" ||
            attribute.ConstructorArguments[1].Value is not Type type)
        {
            error = "LolcodeModuleAttribute must specify a module name and export type.";
            return false;
        }

        name = alias;
        exportType = type;
        return true;
    }

    private static bool IsValidExportType(Type type, out string? error)
    {
        error = null;
        if (!type.IsPublic || type.IsNested)
        {
            error = "the type must be public and non-nested";
            return false;
        }

        bool generated = type.GetCustomAttributesData().Any(attribute =>
            attribute.AttributeType.FullName == LibraryAttributeName &&
            attribute.AttributeType.Assembly.GetName().Name == "Lolcode.Runtime");
        if (generated)
        {
            MethodInfo[] factories = type.GetMethods(
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(method => method.Name == "__CreateLolcodeLibrary")
                .ToArray();
            if (factories is not [var factory] ||
                factory.IsGenericMethodDefinition ||
                factory.ContainsGenericParameters ||
                factory.GetParameters() is not [{ ParameterType.FullName: "Lolcode.Runtime.LolScope" }] ||
                factory.ReturnType.FullName != "Lolcode.Runtime.LolObject")
            {
                error = "the generated library type must expose exactly one non-generic " +
                    "public static __CreateLolcodeLibrary(LolScope) factory returning LolObject";
                return false;
            }
            return true;
        }

        if (!type.IsClass || !type.IsAbstract || !type.IsSealed)
        {
            error = "the type must be a static class or generated LOLCODE library export";
            return false;
        }

        return true;
    }

    private static bool IsValidIdentifier(string value) =>
        value.Length > 0 &&
        char.IsLetter(value[0]) &&
        value.All(static character => char.IsLetterOrDigit(character) || character == '_');

    private static Diagnostic CreateDiagnostic(string path, string message) =>
        Diagnostic.Create(
            DiagnosticDescriptors.InvalidLibraryProvider,
            new TextLocation(path, default, 0, 0, 0, 0),
            message);
}
