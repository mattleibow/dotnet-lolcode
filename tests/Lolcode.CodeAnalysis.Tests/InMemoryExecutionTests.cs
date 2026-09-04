using Lolcode.CodeAnalysis.Scripting;
using Lolcode.CodeAnalysis.Syntax;
using Lolcode.Runtime;

namespace Lolcode.CodeAnalysis.Tests;

public sealed class InMemoryExecutionTests
{
    private const string HelloProgram = """
        HAI 1.2
          VISIBLE "HAI FROM MEMORY"
        KTHXBYE
        """;

    [Fact]
    public void Emit_ToStreams_CreatesNoFiles()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var sourcePath = Path.Combine(tempDirectory, "memory.lol");
            var compilation = LolcodeCompilation.Create(SyntaxTree.ParseText(HelloProgram, sourcePath));
            using var peStream = new MemoryStream();
            using var pdbStream = new MemoryStream();

            var result = compilation.Emit(peStream, pdbStream);

            result.Success.Should().BeTrue();
            result.OutputPath.Should().BeNull();
            result.PdbPath.Should().BeNull();
            peStream.Length.Should().BeGreaterThan(0);
            pdbStream.Length.Should().BeGreaterThan(0);
            Directory.EnumerateFileSystemEntries(tempDirectory).Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Run_CreatesNoFiles()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var result = LolcodeScript.Run(
                HelloProgram,
                filePath: Path.Combine(tempDirectory, "submission.lol"));

            result.Success.Should().BeTrue();
            Directory.EnumerateFileSystemEntries(tempDirectory).Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Run_CapturesVisibleOutput()
    {
        var result = LolcodeScript.Run(HelloProgram);

        result.Success.Should().BeTrue();
        result.Executed.Should().BeTrue();
        result.Diagnostics.Should().NotContain(d => d.Severity == DiagnosticSeverity.Error);
        result.Output.Should().Be($"HAI FROM MEMORY{Environment.NewLine}");
        result.ReturnValue.Should().BeNull();
        result.Exception.Should().BeNull();
    }

    [Fact]
    public void Run_SuppliesInputToGimmeh()
    {
        var result = LolcodeScript.Run(
            """
            HAI 1.2
              I HAS A name
              GIMMEH name
              VISIBLE "HAI, " name "!"
            KTHXBYE
            """,
            standardInput: $"LOLCAT{Environment.NewLine}");

        result.Success.Should().BeTrue();
        result.Output.Should().Be($"HAI, LOLCAT!{Environment.NewLine}");
    }

    [Fact]
    public void Run_CompilationDiagnosticsPreventExecution()
    {
        var result = LolcodeScript.Run(
            """
            HAI 1.2
              VISIBLE missing
            KTHXBYE
            """);

        result.Success.Should().BeFalse();
        result.Executed.Should().BeFalse();
        result.Diagnostics.Should().Contain(d => d.Severity == DiagnosticSeverity.Error);
        result.Output.Should().BeEmpty();
        result.Exception.Should().BeNull();
    }

    [Fact]
    public void Run_UnwrapsRuntimeExceptions()
    {
        var result = LolcodeScript.Run(
            """
            HAI 1.2
              I HAS A value
              VISIBLE SUM OF value AN 1
            KTHXBYE
            """);

        result.Success.Should().BeFalse();
        result.Executed.Should().BeTrue();
        result.Exception.Should().BeOfType<LolRuntimeException>()
            .Which.Message.Should().Contain("NOOB");
        result.Exception.Should().NotBeOfType<System.Reflection.TargetInvocationException>();
    }

    [Fact]
    public void Run_ExecutesFunctionsAndControlFlow()
    {
        var result = LolcodeScript.Run(
            """
            HAI 1.2
              HOW IZ I factorial YR n
                BOTH SAEM n AN 0
                O RLY?
                  YA RLY
                    FOUND YR 1
                OIC
                FOUND YR PRODUKT OF n AN I IZ factorial YR DIFF OF n AN 1 MKAY
              IF U SAY SO

              VISIBLE I IZ factorial YR 5 MKAY
            KTHXBYE
            """);

        result.Success.Should().BeTrue();
        result.Output.Should().Be($"120{Environment.NewLine}");
    }

    [Fact]
    public void Run_RepeatedExecutionsSucceed()
    {
        var results = Enumerable.Range(0, 20)
            .Select(_ => LolcodeScript.Run(HelloProgram))
            .ToArray();

        results.Should().OnlyContain(result => result.Success);
        results.Select(result => result.Output)
            .Should().OnlyContain(output => output == $"HAI FROM MEMORY{Environment.NewLine}");
    }

    [Fact]
    public void Run_NonCollectibleLoaderSupportsRepeatedExecution()
    {
        var compilation = LolcodeCompilation.Create(SyntaxTree.ParseText(HelloProgram));

        var results = Enumerable.Range(0, 3)
            .Select(_ => LolcodeScript.RunCore(
                compilation,
                standardInput: null,
                useNonCollectibleAssemblyLoad: true))
            .ToArray();

        results.Should().OnlyContain(result => result.Success);
        results.Select(result => result.Output)
            .Should().OnlyContain(output => output == $"HAI FROM MEMORY{Environment.NewLine}");
    }

    [Fact]
    public async Task Run_ParallelExecutionsKeepInputAndOutputScoped()
    {
        const string program = """
            HAI 1.2
              I HAS A value
              GIMMEH value
              VISIBLE value
            KTHXBYE
            """;

        var executions = Enumerable.Range(0, 12)
            .Select(index => Task.Run(() => LolcodeScript.Run(
                program,
                standardInput: $"LOLCAT {index}{Environment.NewLine}")))
            .ToArray();

        var results = await Task.WhenAll(executions);

        results.Should().OnlyContain(result => result.Success);
        results.Select(result => result.Output)
            .Should().BeEquivalentTo(
                Enumerable.Range(0, 12).Select(index => $"LOLCAT {index}{Environment.NewLine}"));
    }

    [Fact]
    public void Emit_ToPath_PreservesDllPdbAndRuntimeConfigBehavior()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var sourcePath = Path.Combine(tempDirectory, "program.lol");
            var outputPath = Path.Combine(tempDirectory, "program.dll");
            var compilation = LolcodeCompilation.Create(SyntaxTree.ParseText(HelloProgram, sourcePath));

            var result = compilation.Emit(outputPath, typeof(LolRuntime).Assembly.Location);

            result.Success.Should().BeTrue();
            result.OutputPath.Should().Be(outputPath);
            result.PdbPath.Should().Be(Path.ChangeExtension(outputPath, ".pdb"));
            File.Exists(outputPath).Should().BeTrue();
            File.Exists(Path.ChangeExtension(outputPath, ".pdb")).Should().BeTrue();
            File.Exists(Path.ChangeExtension(outputPath, ".runtimeconfig.json")).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Emit_ToPath_ReportsRuntimeLoadFailuresAsDiagnostics()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var outputPath = Path.Combine(tempDirectory, "program.dll");
            var compilation = LolcodeCompilation.Create(SyntaxTree.ParseText(HelloProgram));

            var result = compilation.Emit(outputPath, Path.Combine(tempDirectory, "missing-runtime.dll"));

            result.Success.Should().BeFalse();
            result.Diagnostics.Should().Contain(d => d.Id == "LOL9001");
            File.Exists(outputPath).Should().BeFalse();
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "lolcode-in-memory-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
