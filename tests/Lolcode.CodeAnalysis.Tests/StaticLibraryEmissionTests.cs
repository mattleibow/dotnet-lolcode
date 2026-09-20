using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Lolcode.CodeAnalysis.Syntax;
using Lolcode.Runtime;
using Lolcode.Runtime.String;

namespace Lolcode.CodeAnalysis.Tests;

public class StaticLibraryEmissionTests
{
    [Fact]
    public void StaticProviderEmission_ReferencesFactoryWithoutDynamicLoader()
    {
        string outputPath = Path.Combine(
            AppContext.BaseDirectory,
            $"static-provider-{Guid.NewGuid():N}.dll");
        try
        {
            var compilation = LolcodeCompilation.Create(SyntaxTree.ParseText(
                """
                HAI 1.4
                  CAN HAS STRING?
                  VISIBLE I IZ STRING'Z LEN YR "HAI" MKAY
                KTHXBYE
                """,
                "StaticProvider.lol"));

            EmitResult result = compilation.Emit(
                outputPath,
                typeof(LolRuntime).Assembly.Location,
                GetReferenceAssemblyPaths()
                    .Append(typeof(LolRuntime).Assembly.Location)
                    .Append(typeof(StringLibraryFactory).Assembly.Location),
                libraryDescriptors:
                [
                    "STRING|Lolcode.Runtime.String|Lolcode.Runtime.String.StringLibrary|true|1|Lolcode.Runtime.String.StringLibraryFactory"
                ],
                options: new LolcodeEmitOptions
                {
                    LibraryResolution = LolcodeLibraryResolution.Static,
                    RuntimeConfigOwnedByHost = true,
                });

            result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
            using var stream = File.OpenRead(outputPath);
            using var peReader = new PEReader(stream);
            MetadataReader metadata = peReader.GetMetadataReader();
            GetReferencedMethods(metadata)
                .Should()
                .Contain(("Lolcode.Runtime.String", "StringLibraryFactory", "Create"));
            GetReferencedMethods(metadata)
                .Should()
                .NotContain(method =>
                    method.Namespace == "Lolcode.Runtime" &&
                    method.Type == "LolRuntime" &&
                    method.Method == "LoadLibrary");
        }
        finally
        {
            DeleteEmitArtifacts(outputPath);
        }
    }

    [Fact]
    public void StaticGeneratedLibraryEmission_ReferencesGeneratedFactory()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "lolcode-static-library-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string libraryPath = Path.Combine(directory, "StaticModule.dll");
            var libraryCompilation = LolcodeCompilation.Create(SyntaxTree.ParseText(
                """
                HAI 1.4
                HOW IZ I GREETING
                  FOUND YR "HAI"
                IF U SAY SO
                KTHXBYE
                """,
                "StaticModule.lol"));
            EmitResult libraryResult = libraryCompilation.Emit(
                libraryPath,
                typeof(LolRuntime).Assembly.Location,
                GetReferenceAssemblyPaths().Append(typeof(LolRuntime).Assembly.Location),
                outputType: "Library",
                libraryTypeName: "StaticModule.Exports",
                options: new LolcodeEmitOptions
                {
                    LibraryResolution = LolcodeLibraryResolution.Static,
                    RuntimeConfigOwnedByHost = true,
                });
            libraryResult.Success.Should().BeTrue(
                string.Join(Environment.NewLine, libraryResult.Diagnostics));

            string appPath = Path.Combine(directory, "StaticConsumer.dll");
            var appCompilation = LolcodeCompilation.Create(SyntaxTree.ParseText(
                """
                HAI 1.4
                  CAN HAS StaticModule?
                  VISIBLE I IZ StaticModule'Z GREETING MKAY
                KTHXBYE
                """,
                "StaticConsumer.lol"));
            EmitResult appResult = appCompilation.Emit(
                appPath,
                typeof(LolRuntime).Assembly.Location,
                GetReferenceAssemblyPaths()
                    .Append(typeof(LolRuntime).Assembly.Location)
                    .Append(libraryPath),
                options: new LolcodeEmitOptions
                {
                    LibraryResolution = LolcodeLibraryResolution.Static,
                    RuntimeConfigOwnedByHost = true,
                });

            appResult.Success.Should().BeTrue(
                string.Join(Environment.NewLine, appResult.Diagnostics));
            using var stream = File.OpenRead(appPath);
            using var peReader = new PEReader(stream);
            GetReferencedMethods(peReader.GetMetadataReader())
                .Should()
                .Contain(("StaticModule", "Exports", "__CreateLolcodeLibrary"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void StaticEmission_ReportsUndeclaredImport()
    {
        string outputPath = Path.Combine(
            AppContext.BaseDirectory,
            $"static-missing-{Guid.NewGuid():N}.dll");
        try
        {
            var compilation = LolcodeCompilation.Create(SyntaxTree.ParseText(
                """
                HAI 1.4
                  CAN HAS MISSING?
                KTHXBYE
                """,
                "MissingStaticLibrary.lol"));

            EmitResult result = compilation.Emit(
                outputPath,
                typeof(LolRuntime).Assembly.Location,
                GetReferenceAssemblyPaths().Append(typeof(LolRuntime).Assembly.Location),
                options: new LolcodeEmitOptions
                {
                    LibraryResolution = LolcodeLibraryResolution.Static,
                    RuntimeConfigOwnedByHost = true,
                });

            result.Success.Should().BeFalse();
            result.Diagnostics.Should().ContainSingle(diagnostic =>
                diagnostic.Id == "LOL3001" &&
                diagnostic.Location.FileName == "MissingStaticLibrary.lol");
            File.Exists(outputPath).Should().BeFalse();
        }
        finally
        {
            DeleteEmitArtifacts(outputPath);
        }
    }

    private static IEnumerable<(string Namespace, string Type, string Method)> GetReferencedMethods(
        MetadataReader metadata)
    {
        foreach (MemberReferenceHandle handle in metadata.MemberReferences)
        {
            MemberReference member = metadata.GetMemberReference(handle);
            if (member.Parent.Kind != HandleKind.TypeReference)
                continue;

            TypeReference type = metadata.GetTypeReference((TypeReferenceHandle)member.Parent);
            yield return (
                metadata.GetString(type.Namespace),
                metadata.GetString(type.Name),
                metadata.GetString(member.Name));
        }
    }

    private static IEnumerable<string> GetReferenceAssemblyPaths()
    {
        string runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)
            ?? throw new InvalidOperationException("Could not locate the current runtime directory.");
        string dotnetRoot = Directory.GetParent(
            Directory.GetParent(
                Directory.GetParent(runtimeDirectory)?.FullName
                ?? throw new InvalidOperationException("Could not locate the shared framework directory."))
            ?.FullName
            ?? throw new InvalidOperationException("Could not locate the shared framework root."))
            ?.FullName
            ?? throw new InvalidOperationException("Could not locate the .NET installation root.");
        string referencePackRoot = Path.Combine(dotnetRoot, "packs", "Microsoft.NETCore.App.Ref");
        string referencePack = Directory.EnumerateDirectories(referencePackRoot, "10.*")
            .OrderByDescending(path => path, StringComparer.Ordinal)
            .First();
        return Directory.EnumerateFiles(
            Path.Combine(referencePack, "ref", "net10.0"),
            "*.dll");
    }

    private static void DeleteEmitArtifacts(string outputPath)
    {
        File.Delete(outputPath);
        File.Delete(Path.ChangeExtension(outputPath, ".pdb"));
        File.Delete(Path.ChangeExtension(outputPath, ".runtimeconfig.json"));
    }
}
