using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Lolcode.CodeAnalysis.Syntax;
using Lolcode.Runtime;

namespace Lolcode.CodeAnalysis.Tests;

/// <summary>Tests CLR library emission and its public LOLCODE function ABI.</summary>
public class LibraryEmissionTests
{
    [Fact]
    public void LibraryEmission_HasNoEntryPointOrRuntimeConfig_AndExposesTopLevelFunction()
    {
        string assemblyName = $"library-{Guid.NewGuid():N}";
        string outputPath = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.dll");
        string runtimeConfigPath = Path.ChangeExtension(outputPath, ".runtimeconfig.json");
        try
        {
            var compilation = LolcodeCompilation.Create(SyntaxTree.ParseText("""
                HAI 1.4
                HOW IZ I ADD YR left AN YR right
                    FOUND YR SUM OF left AN right
                IF U SAY SO
                KTHXBYE
                """));

            var result = compilation.Emit(outputPath, typeof(LolRuntime).Assembly.Location, "Library");

            result.Success.Should().BeTrue();
            File.Exists(runtimeConfigPath).Should().BeFalse();

            using var stream = File.OpenRead(outputPath);
            using var peReader = new PEReader(stream);
            peReader.PEHeaders.CorHeader!.EntryPointTokenOrRelativeVirtualAddress.Should().Be(0);
            peReader.PEHeaders.CoffHeader.Characteristics.Should().HaveFlag(Characteristics.Dll);

            MetadataReader metadata = peReader.GetMetadataReader();
            TypeDefinitionHandle exportsHandle = metadata.TypeDefinitions.Single(handle =>
                metadata.GetString(metadata.GetTypeDefinition(handle).Name) == "LolcodeExports");
            bool hasPublicAdd = metadata.GetTypeDefinition(exportsHandle)
                .GetMethods()
                .Select(metadata.GetMethodDefinition)
                .Any(method => metadata.GetString(method.Name) == "ADD" &&
                    method.Attributes.HasFlag(MethodAttributes.Public) &&
                    method.Attributes.HasFlag(MethodAttributes.Static));
            hasPublicAdd.Should().BeTrue();

            Type emittedExports = Assembly.LoadFrom(outputPath).GetType("LolcodeExports")!;
            CustomAttributeData libraryAttribute = emittedExports.CustomAttributes.Single(attribute =>
                attribute.AttributeType == typeof(LolcodeLibraryAttribute));
            libraryAttribute.ConstructorArguments.Should().BeEmpty();
        }
        finally
        {
            File.Delete(outputPath);
            File.Delete(runtimeConfigPath);
        }
    }

    [Fact]
    public void LibraryEmission_UsesTargetFrameworkCoreAssembly()
    {
        string outputPath = Path.Combine(AppContext.BaseDirectory, $"library-{Guid.NewGuid():N}.dll");
        try
        {
            var compilation = LolcodeCompilation.Create(SyntaxTree.ParseText("""
                HAI 1.4
                HOW IZ I ADD YR left AN YR right
                    FOUND YR SUM OF left AN right
                IF U SAY SO
                KTHXBYE
                """));

            var result = compilation.Emit(
                outputPath,
                typeof(LolRuntime).Assembly.Location,
                GetNet10ReferenceAssemblyPaths(),
                "Library",
                "InteropSamples.TargetExports");

            result.Success.Should().BeTrue();

            using var stream = File.OpenRead(outputPath);
            using var peReader = new PEReader(stream);
            MetadataReader metadata = peReader.GetMetadataReader();
            metadata.TypeDefinitions
                .Select(metadata.GetTypeDefinition)
                .Select(definition => metadata.GetString(definition.Namespace) + "." +
                    metadata.GetString(definition.Name))
                .Should().Contain("InteropSamples.TargetExports");
            var references = metadata.AssemblyReferences
                .Select(handle => metadata.GetAssemblyReference(handle))
                .Select(reference => new
                {
                    Name = metadata.GetString(reference.Name),
                    reference.Version,
                })
                .ToArray();

            references.Should().Contain(reference =>
                reference.Name == "System.Runtime" && reference.Version.Major == 10);
            references.Should().NotContain(reference => reference.Name == "System.Private.CoreLib");
        }
        finally
        {
            File.Delete(outputPath);
        }
    }

    private static IEnumerable<string> GetNet10ReferenceAssemblyPaths()
    {
        string runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)
            ?? throw new InvalidOperationException("Could not locate the current runtime directory.");
        string dotnetRoot = Directory.GetParent(
            Directory.GetParent(
                Directory.GetParent(runtimeDirectory)?.FullName
                ?? throw new InvalidOperationException("Could not locate the .NET shared framework directory."))
            ?.FullName
            ?? throw new InvalidOperationException("Could not locate the .NET shared framework root."))
            ?.FullName
            ?? throw new InvalidOperationException("Could not locate the .NET installation root.");
        string referencePackRoot = Path.Combine(dotnetRoot, "packs", "Microsoft.NETCore.App.Ref");
        string referencePack = Directory.EnumerateDirectories(referencePackRoot, "10.*")
            .OrderByDescending(path => path, StringComparer.Ordinal)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("Could not locate a .NET 10 reference pack.");
        string referenceDirectory = Path.Combine(referencePack, "ref", "net10.0");

        return Directory.EnumerateFiles(referenceDirectory, "*.dll");
    }
}
