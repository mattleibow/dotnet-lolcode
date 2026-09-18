using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Lolcode.CodeAnalysis.Syntax;
using Lolcode.Runtime;

namespace Lolcode.CodeAnalysis.Tests;

/// <summary>Tests CLR class library emission and the public LOLCODE function ABI.</summary>
public class LibraryEmissionTests
{
    [Fact]
    public void LibraryEmission_HasLibraryHeaderNoEntryPointAndPublicObjectAbi()
    {
        string assemblyName = $"library-{Guid.NewGuid():N}";
        string outputPath = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.dll");
        string runtimeConfigPath = Path.ChangeExtension(outputPath, ".runtimeconfig.json");
        try
        {
            var compilation = LolcodeCompilation.Create(SyntaxTree.ParseText(
                """
                HAI 1.4
                HOW IZ I WELCOME YR name AN YR cheezburgerz
                    FOUND YR SMOOSH "HAI " AN name AN ", U CAN HAZ " AN cheezburgerz AN " CHEEZBURGERZ!" MKAY
                IF U SAY SO
                KTHXBYE
                """,
                "Welcome.lol"));

            var result = compilation.Emit(
                outputPath,
                typeof(LolRuntime).Assembly.Location,
                outputType: "Library");

            result.Success.Should().BeTrue();
            result.PdbPath.Should().Be(Path.ChangeExtension(outputPath, ".pdb"));
            File.Exists(runtimeConfigPath).Should().BeFalse();

            using var stream = File.OpenRead(outputPath);
            using var peReader = new PEReader(stream);
            peReader.PEHeaders.CorHeader!.EntryPointTokenOrRelativeVirtualAddress.Should().Be(0);
            peReader.PEHeaders.CoffHeader.Characteristics.Should().HaveFlag(Characteristics.Dll);

            MetadataReader metadata = peReader.GetMetadataReader();
            TypeDefinitionHandle exportsHandle = metadata.TypeDefinitions.Single(handle =>
            {
                TypeDefinition definition = metadata.GetTypeDefinition(handle);
                return metadata.GetString(definition.Name) == "LolcodeExports" &&
                    metadata.GetString(definition.Namespace) == string.Empty;
            });
            MethodDefinition[] welcomeMethods = metadata.GetTypeDefinition(exportsHandle)
                .GetMethods()
                .Select(metadata.GetMethodDefinition)
                .Where(method => metadata.GetString(method.Name) == "WELCOME")
                .ToArray();

            welcomeMethods.Should().ContainSingle(method =>
                method.Attributes.HasFlag(MethodAttributes.Public) &&
                method.Attributes.HasFlag(MethodAttributes.Static));
            welcomeMethods.Should().ContainSingle(method =>
                method.Attributes.HasFlag(MethodAttributes.Private) &&
                method.Attributes.HasFlag(MethodAttributes.Static));

            Type exports = Assembly.LoadFrom(outputPath).GetType("LolcodeExports")!;
            MethodInfo wrapper = exports.GetMethod(
                "WELCOME",
                BindingFlags.Public | BindingFlags.Static)!;
            wrapper.ReturnType.Should().Be(typeof(object));
            wrapper.GetParameters()
                .Select(parameter => parameter.ParameterType)
                .Should()
                .Equal(typeof(object), typeof(object));

            using var pdbStream = File.OpenRead(result.PdbPath!);
            using var pdbProvider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
            MethodDefinitionHandle privateImplementation = metadata.GetTypeDefinition(exportsHandle)
                .GetMethods()
                .Single(handle =>
                {
                    MethodDefinition method = metadata.GetMethodDefinition(handle);
                    return metadata.GetString(method.Name) == "WELCOME" &&
                        method.Attributes.HasFlag(MethodAttributes.Private);
                });
            pdbProvider.GetMetadataReader()
                .GetMethodDebugInformation(privateImplementation)
                .SequencePointsBlob
                .IsNil
                .Should()
                .BeFalse();
        }
        finally
        {
            File.Delete(outputPath);
            File.Delete(Path.ChangeExtension(outputPath, ".pdb"));
            File.Delete(runtimeConfigPath);
        }
    }

    [Fact]
    public void LibraryEmission_UsesSuppliedTargetFrameworkReferenceAssemblies()
    {
        string outputPath = Path.Combine(AppContext.BaseDirectory, $"library-{Guid.NewGuid():N}.dll");
        try
        {
            var compilation = LolcodeCompilation.Create(SyntaxTree.ParseText(
                """
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
                .Should()
                .Contain(definition =>
                    metadata.GetString(definition.Namespace) == "InteropSamples" &&
                    metadata.GetString(definition.Name) == "TargetExports");
            metadata.AssemblyReferences
                .Select(metadata.GetAssemblyReference)
                .Should()
                .Contain(reference =>
                    metadata.GetString(reference.Name) == "System.Runtime" &&
                    reference.Version.Major == 10);
            metadata.AssemblyReferences
                .Select(metadata.GetAssemblyReference)
                .Select(reference => metadata.GetString(reference.Name))
                .Should()
                .NotContain("System.Private.CoreLib");
        }
        finally
        {
            File.Delete(outputPath);
            File.Delete(Path.ChangeExtension(outputPath, ".pdb"));
        }
    }

    [Fact]
    public void LibraryEmission_RemovesExistingRuntimeConfigAsPartOfArtifactCommit()
    {
        string outputDirectory = Path.Combine(
            Path.GetTempPath(),
            "lolcode-library-emission-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);

        try
        {
            string outputPath = Path.Combine(outputDirectory, "library.dll");
            string pdbPath = Path.ChangeExtension(outputPath, ".pdb");
            string runtimeConfigPath = Path.ChangeExtension(outputPath, ".runtimeconfig.json");
            File.WriteAllText(outputPath, "old dll");
            File.WriteAllText(pdbPath, "old pdb");
            File.WriteAllText(runtimeConfigPath, "old runtimeconfig");

            var compilation = LolcodeCompilation.Create(SyntaxTree.ParseText(
                """
                HAI 1.4
                HOW IZ I WELCOME
                    FOUND YR "HAI"
                IF U SAY SO
                KTHXBYE
                """,
                Path.Combine(outputDirectory, "Welcome.lol")));
            var fileSystem = new RuntimeConfigDeletionFailureFileSystem(runtimeConfigPath);

            var result = compilation.Emit(
                outputPath,
                typeof(LolRuntime).Assembly.Location,
                fileSystem,
                outputType: "Library");

            result.Success.Should().BeTrue();
            File.Exists(outputPath).Should().BeTrue();
            File.Exists(pdbPath).Should().BeTrue();
            File.Exists(runtimeConfigPath).Should().BeFalse();
            Directory.EnumerateFiles(outputDirectory)
                .Should()
                .NotContain(path => path.EndsWith(".tmp", StringComparison.Ordinal)
                    || path.EndsWith(".bak", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(outputDirectory, recursive: true);
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
            .First();
        return Directory.EnumerateFiles(Path.Combine(referencePack, "ref", "net10.0"), "*.dll");
    }

    private sealed class RuntimeConfigDeletionFailureFileSystem(string runtimeConfigPath)
        : IPathEmitFileSystem
    {
        public bool FileExists(string path) => File.Exists(path);

        public void CreateDirectory(string path) => Directory.CreateDirectory(path);

        public Stream CreateNewFile(string path)
            => new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);

        public void MoveFile(string sourcePath, string destinationPath, bool overwrite)
            => File.Move(sourcePath, destinationPath, overwrite);

        public void DeleteFile(string path)
        {
            if (path == runtimeConfigPath)
                throw new IOException("Injected runtimeconfig deletion failure.");

            File.Delete(path);
        }
    }
}
