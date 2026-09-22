using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Lolcode.CodeAnalysis.Syntax;
using Lolcode.Runtime;

namespace Lolcode.CodeAnalysis.Tests;

/// <summary>Tests CLR class library emission and the public LOLCODE function ABI.</summary>
public class LibraryEmissionTests
{
    private static string CompatibilityFixtureSource(params string[] path) =>
        File.ReadAllText(Path.Combine([AppContext.BaseDirectory, "Compatibility", .. path]));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LibraryEmission_MultipleParameterlessWrappersInitializeObjectsIndependently(bool emitPdb)
    {
        string assemblyName = $"library-wrappers-{Guid.NewGuid():N}";
        string outputPath = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.dll");
        string? sourcePath = emitPdb ? "LibraryWrappers.lol" : null;
        try
        {
            var compilation = LolcodeCompilation.Create(SyntaxTree.ParseText(
                CompatibilityFixtureSource("DotNet", "1.4", "CodeAnalysis", "library-wrappers", "test.lol"),
                sourcePath));

            var result = compilation.Emit(
                outputPath,
                typeof(LolRuntime).Assembly.Location,
                outputType: "Library");

            result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
            (result.PdbPath is not null).Should().Be(emitPdb);

            var loadContext = new System.Runtime.Loader.AssemblyLoadContext(
                $"LibraryWrappers_{Guid.NewGuid():N}",
                isCollectible: true);
            loadContext.Resolving += (_, assemblyName) =>
                AssemblyName.ReferenceMatchesDefinition(
                    assemblyName,
                    typeof(LolRuntime).Assembly.GetName())
                    ? typeof(LolRuntime).Assembly
                    : null;
            try
            {
                var assembly = LoadAssemblyFromCopiedStreams(
                    loadContext,
                    outputPath,
                    result.PdbPath);
                Type exports = assembly.GetType("LolcodeExports")!;
                using var first = (IDisposable)Activator.CreateInstance(exports)!;
                using var second = (IDisposable)Activator.CreateInstance(exports)!;
                exports.GetMethod("FIRST")!.Invoke(first, null).Should().Be("FIRST");
                exports.GetMethod("SECOND")!.Invoke(second, null).Should().Be("SECOND");
            }
            finally
            {
                loadContext.Unload();
            }
        }
        finally
        {
            File.Delete(outputPath);
            File.Delete(Path.ChangeExtension(outputPath, ".pdb"));
        }
    }

    [Fact]
    public void LibraryEmission_HasLibraryHeaderNoEntryPointAndPublicObjectAbi()
    {
        string assemblyName = $"library-{Guid.NewGuid():N}";
        string outputPath = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.dll");
        string runtimeConfigPath = Path.ChangeExtension(outputPath, ".runtimeconfig.json");
        try
        {
            var compilation = LolcodeCompilation.Create(SyntaxTree.ParseText(
                CompatibilityFixtureSource("DotNet", "1.4", "Sdk", "library-welcome", "test.lol"),
                "Welcome.lol"));

            var result = compilation.Emit(
                outputPath,
                typeof(LolRuntime).Assembly.Location,
                outputType: "Library");

            result.Success.Should().BeTrue();
            result.PdbPath.Should().Be(Path.ChangeExtension(outputPath, ".pdb"));
            File.Exists(runtimeConfigPath).Should().BeFalse();

            using (var stream = File.OpenRead(outputPath))
            using (var peReader = new PEReader(stream))
            {
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
                    !method.Attributes.HasFlag(MethodAttributes.Static));
                welcomeMethods.Should().ContainSingle(method =>
                    method.Attributes.HasFlag(MethodAttributes.Private) &&
                    method.Attributes.HasFlag(MethodAttributes.Static));

                MethodDefinition wrapper = welcomeMethods.Single(method =>
                    method.Attributes.HasFlag(MethodAttributes.Public) &&
                    !method.Attributes.HasFlag(MethodAttributes.Static));
                metadata.GetBlobBytes(wrapper.Signature)
                    .Should()
                    .Equal(0x20, 0x02, 0x1C, 0x1C, 0x1C);

                using (var pdbStream = File.OpenRead(result.PdbPath!))
                using (var pdbProvider = MetadataReaderProvider.FromPortablePdbStream(pdbStream))
                {
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
            }
        }
        finally
        {
            File.Delete(outputPath);
            File.Delete(Path.ChangeExtension(outputPath, ".pdb"));
            File.Delete(runtimeConfigPath);
            File.Exists(outputPath).Should().BeFalse();
            File.Exists(Path.ChangeExtension(outputPath, ".pdb")).Should().BeFalse();
            File.Exists(runtimeConfigPath).Should().BeFalse();
        }
    }

    [Fact]
    public void LibraryEmission_UsesSuppliedTargetFrameworkReferenceAssemblies()
    {
        string outputPath = Path.Combine(AppContext.BaseDirectory, $"library-{Guid.NewGuid():N}.dll");
        try
        {
            var compilation = LolcodeCompilation.Create(SyntaxTree.ParseText(
                CompatibilityFixtureSource("DotNet", "1.4", "CodeAnalysis", "library-reference-assemblies", "test.lol")));

            var result = compilation.Emit(
                outputPath,
                typeof(LolRuntime).Assembly.Location,
                GetNet10ReferenceAssemblyPaths(),
                "Library",
                "InteropSamples.TargetExports");

            result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));

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
        }
        finally
        {
            File.Delete(outputPath);
            File.Delete(Path.ChangeExtension(outputPath, ".pdb"));
        }
    }

    [Fact]
    public void LibraryEmission_ExportsFunctionsDeclaredInMultipleSourceFiles()
    {
        string outputPath = Path.Combine(AppContext.BaseDirectory, $"multifile-library-{Guid.NewGuid():N}.dll");
        try
        {
            var compilation = LolcodeCompilation.Create(
                SyntaxTree.ParseText(
                    CompatibilityFixtureSource("DotNet", "1.4", "CodeAnalysis", "library-multifile-exports", "first.lol"),
                    "First.lol"),
                SyntaxTree.ParseText(
                    CompatibilityFixtureSource("DotNet", "1.4", "CodeAnalysis", "library-multifile-exports", "second.lol"),
                    "Second.lol"));

            var result = compilation.Emit(
                outputPath,
                typeof(LolRuntime).Assembly.Location,
                outputType: "Library");

            result.Success.Should().BeTrue(string.Join("\n", result.Diagnostics));
            using var stream = File.OpenRead(outputPath);
            using var peReader = new PEReader(stream);
            MetadataReader metadata = peReader.GetMetadataReader();
            TypeDefinitionHandle exports = metadata.TypeDefinitions.Single(handle =>
                metadata.GetString(metadata.GetTypeDefinition(handle).Name) == "LolcodeExports");
            metadata.GetTypeDefinition(exports)
                .GetMethods()
                .Select(metadata.GetMethodDefinition)
                .Where(method => method.Attributes.HasFlag(MethodAttributes.Public))
                .Select(method => metadata.GetString(method.Name))
                .Should().Contain(["FIRST", "SECOND"]);
        }
        finally
        {
            File.Delete(outputPath);
            File.Delete(Path.ChangeExtension(outputPath, ".pdb"));
        }
    }

    [Fact]
    public void LibraryEmission_HoistsRuntimeFunctionValuesBeforeWrapperInitialization()
    {
        string outputPath = Path.Combine(
            AppContext.BaseDirectory,
            $"runtime-function-library-{Guid.NewGuid():N}.dll");
        try
        {
            var compilation = LolcodeCompilation.Create(
                SyntaxTree.ParseText(
                    CompatibilityFixtureSource("DotNet", "1.4", "CodeAnalysis", "library-runtime-function-values", "welcome.lol"),
                    "Welcome.lol"),
                SyntaxTree.ParseText(
                    CompatibilityFixtureSource("DotNet", "1.4", "CodeAnalysis", "library-runtime-function-values", "greeting.lol"),
                    "Greeting.lol"));

            var result = compilation.Emit(
                outputPath,
                typeof(LolRuntime).Assembly.Location,
                outputType: "Library");

            result.Success.Should().BeTrue(string.Join("\n", result.Diagnostics));
            var loadContext = new System.Runtime.Loader.AssemblyLoadContext(
                $"RuntimeFunctionLibrary_{Guid.NewGuid():N}",
                isCollectible: true);
            loadContext.Resolving += (_, assemblyName) =>
                AssemblyName.ReferenceMatchesDefinition(
                    assemblyName,
                    typeof(LolRuntime).Assembly.GetName())
                    ? typeof(LolRuntime).Assembly
                    : null;
            try
            {
                var assembly = LoadAssemblyFromCopiedStreams(
                    loadContext,
                    outputPath,
                    result.PdbPath);
                Type exports = assembly.GetType("LolcodeExports")!;
                var instance = (IDisposable)Activator.CreateInstance(exports)!;
                try
                {
                    var library = (LolObject)exports.GetProperty("Library")!.GetValue(instance)!;
                    LolRuntime.GetValue(library, ["greeting"]).Should().Be("HAI");
                }
                finally
                {
                    instance.Dispose();
                }

                exports
                    .GetMethod("WELCOME")!
                    .Invoke(Activator.CreateInstance(exports), null)
                    .Should()
                    .Be("HAI");
            }
            finally
            {
                loadContext.Unload();
            }
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
                CompatibilityFixtureSource("DotNet", "1.4", "CodeAnalysis", "library-runtimeconfig-commit", "test.lol"),
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

    private static Assembly LoadAssemblyFromCopiedStreams(
        System.Runtime.Loader.AssemblyLoadContext loadContext,
        string outputPath,
        string? pdbPath)
    {
        byte[] peImage = File.ReadAllBytes(outputPath);
        using var peStream = new MemoryStream(peImage, writable: false);
        if (pdbPath is null)
            return loadContext.LoadFromStream(peStream);

        byte[] pdbImage = File.ReadAllBytes(pdbPath);
        using var pdbStream = new MemoryStream(pdbImage, writable: false);
        return loadContext.LoadFromStream(peStream, pdbStream);
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
