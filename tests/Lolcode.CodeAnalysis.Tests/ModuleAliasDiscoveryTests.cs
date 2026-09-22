using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Lolcode.Runtime;
using System.Reflection;

namespace Lolcode.CodeAnalysis.Tests;

public sealed class ModuleAliasDiscoveryTests : IDisposable
{
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "lolcode-module-alias-tests", Guid.NewGuid().ToString("N"));

    public ModuleAliasDiscoveryTests() => Directory.CreateDirectory(_directory);

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public void DiscoversValidModuleAlias()
    {
        string assemblyPath = CompileAssembly(
            """
            [assembly: Lolcode.Runtime.LolcodeModule("FRIENDLY", typeof(AliasLibrary))]
            public static class AliasLibrary
            {
                public static string ECHO(string value) => value;
            }
            """,
            "ValidAlias");

        ModuleAliasDiscoveryResult result = Discover(assemblyPath);

        result.Diagnostics.Should().BeEmpty();
        result.Aliases.Should().Contain(
            new ModuleAlias("FRIENDLY", "ValidAlias", "AliasLibrary"));
    }

    [Fact]
    public void RejectsReservedBuiltInAlias()
    {
        string assemblyPath = CompileAssembly(
            """
            [assembly: Lolcode.Runtime.LolcodeModule("STRING", typeof(AliasLibrary))]
            public static class AliasLibrary { }
            """,
            "ReservedAlias");

        Diagnostic diagnostic = Discover(assemblyPath).Diagnostics.Should().ContainSingle().Which;

        diagnostic.Id.Should().Be("LOL9003");
        diagnostic.Message.Should().Contain("STRING").And.Contain("reserved");
    }

    [Fact]
    public void RejectsDuplicateAlias()
    {
        string assemblyPath = CompileAssembly(
            """
            [assembly: Lolcode.Runtime.LolcodeModule("DUPLICATE", typeof(FirstLibrary))]
            [assembly: Lolcode.Runtime.LolcodeModule("DUPLICATE", typeof(SecondLibrary))]
            public static class FirstLibrary { }
            public static class SecondLibrary { }
            """,
            "DuplicateAlias");

        Diagnostic diagnostic = Discover(assemblyPath).Diagnostics.Should().ContainSingle().Which;

        diagnostic.Id.Should().Be("LOL9003");
        diagnostic.Message.Should().Contain("DUPLICATE")
            .And.Contain("FirstLibrary")
            .And.Contain("SecondLibrary");
    }

    [Fact]
    public void RejectsDuplicateAliasAcrossReferences()
    {
        string firstPath = CompileAssembly(
            """
            [assembly: Lolcode.Runtime.LolcodeModule("DUPLICATE", typeof(FirstLibrary))]
            public static class FirstLibrary { }
            """,
            "FirstAlias");
        string secondPath = CompileAssembly(
            """
            [assembly: Lolcode.Runtime.LolcodeModule("DUPLICATE", typeof(SecondLibrary))]
            public static class SecondLibrary { }
            """,
            "SecondAlias");

        Diagnostic diagnostic = Discover(firstPath, secondPath)
            .Diagnostics.Should().ContainSingle().Which;

        diagnostic.Id.Should().Be("LOL9003");
        diagnostic.Message.Should().Contain("FirstAlias:FirstLibrary")
            .And.Contain("SecondAlias:SecondLibrary");
    }

    [Fact]
    public void RejectsInvalidExportType()
    {
        string assemblyPath = CompileAssembly(
            """
            [assembly: Lolcode.Runtime.LolcodeModule("INVALID", typeof(InstanceLibrary))]
            public class InstanceLibrary { }
            """,
            "InvalidAlias");

        Diagnostic diagnostic = Discover(assemblyPath).Diagnostics.Should().ContainSingle().Which;

        diagnostic.Id.Should().Be("LOL9003");
        diagnostic.Message.Should().Contain("INVALID")
            .And.Contain("static class");
    }

    [Fact]
    public void ReportsUnreadableReferenceMetadata()
    {
        string path = Path.Combine(_directory, "NotAnAssembly.dll");
        File.WriteAllText(path, "not an assembly");

        Diagnostic diagnostic = Discover(path).Diagnostics.Should().ContainSingle().Which;

        diagnostic.Id.Should().Be("LOL9003");
        diagnostic.Location.FileName.Should().Be(path);
        diagnostic.Message.Should().Contain("Unable to read module aliases");
    }

    [Fact]
    public void RejectsGeneratedExportWithoutFactory()
    {
        string assemblyPath = CompileAssembly(
            """
            [assembly: Lolcode.Runtime.LolcodeModule("BROKEN", typeof(BrokenLibrary))]
            [Lolcode.Runtime.LolcodeLibrary]
            public static class BrokenLibrary { }
            """,
            "BrokenGeneratedAlias");

        Diagnostic diagnostic = Discover(assemblyPath).Diagnostics.Should().ContainSingle().Which;

        diagnostic.Id.Should().Be("LOL9003");
        diagnostic.Message.Should().Contain("BROKEN")
            .And.Contain("__CreateLolcodeLibrary");
    }

    [Fact]
    public void RejectsGenericGeneratedFactory()
    {
        string assemblyPath = CompileAssembly(
            """
            [assembly: Lolcode.Runtime.LolcodeModule("BROKEN", typeof(BrokenLibrary))]
            [Lolcode.Runtime.LolcodeLibrary]
            public static class BrokenLibrary
            {
                public static Lolcode.Runtime.LolObject __CreateLolcodeLibrary<T>(
                    Lolcode.Runtime.LolScope scope) => new();
            }
            """,
            "GenericGeneratedAlias");

        Diagnostic diagnostic = Discover(assemblyPath).Diagnostics.Should().ContainSingle().Which;

        diagnostic.Id.Should().Be("LOL9003");
        diagnostic.Message.Should().Contain("non-generic");
    }

    [Fact]
    public void StreamEmitUsesResolvedReferencesForFriendlyAliases()
    {
        var compilation = LolcodeCompilation.Create(Lolcode.CodeAnalysis.Syntax.SyntaxTree.ParseText(
            File.ReadAllText(Path.Combine(
                AppContext.BaseDirectory,
                "Compatibility",
                "DotNet",
                "1.4",
                "CodeAnalysis",
                "module-alias-stream",
                "test.lol"))));
        string[] references = GetFrameworkReferencePaths()
            .Append(typeof(LolRuntime).Assembly.Location)
            .Append(typeof(CustomProviderFixture.CustomLibrary).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        using var peStream = new MemoryStream();

        Lolcode.CodeAnalysis.EmitResult result = compilation.Emit(
            peStream,
            pdbStream: null,
            referenceAssemblyPaths: references);

        result.Success.Should().BeTrue(
            string.Join(Environment.NewLine, result.Diagnostics));
        TextWriter original = Console.Out;
        using var output = new StringWriter();
        try
        {
            Console.SetOut(output);
            Assembly assembly = Assembly.Load(peStream.ToArray());
            MethodInfo entryPoint = assembly.EntryPoint!;
            entryPoint.Invoke(
                null,
                entryPoint.GetParameters().Length == 0 ? null : [Array.Empty<string>()]);
        }
        finally
        {
            Console.SetOut(original);
        }
        output.ToString().Trim().Should().Be("CUSTOM STREAM");
    }

    private ModuleAliasDiscoveryResult Discover(params string[] assemblyPaths)
    {
        string[] references = GetFrameworkReferencePaths()
            .Append(typeof(LolRuntime).Assembly.Location)
            .Concat(assemblyPaths)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return ModuleAliasDiscovery.Discover(references, typeof(LolRuntime).Assembly.Location);
    }

    private string CompileAssembly(string source, string assemblyName)
    {
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(source)],
            GetFrameworkReferencePaths()
                .Append(typeof(LolRuntime).Assembly.Location)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        string path = Path.Combine(_directory, $"{assemblyName}.dll");
        var result = compilation.Emit(path);
        result.Success.Should().BeTrue(
            string.Join(Environment.NewLine, result.Diagnostics));
        return path;
    }

    private static IEnumerable<string> GetFrameworkReferencePaths() =>
        ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))!
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
}
