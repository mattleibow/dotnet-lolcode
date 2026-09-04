using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Lolcode.CodeAnalysis;
using Lolcode.CodeAnalysis.Syntax;

namespace Lolcode.EndToEnd.Tests;

[Collection(nameof(LciConformanceCollection))]
public class LciConformanceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _runtimeAssemblyPath;

    public LciConformanceTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(), "lolcode-lci-conformance", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _runtimeAssemblyPath = Path.Combine(AppContext.BaseDirectory, "Lolcode.Runtime.dll");
    }

    public static IEnumerable<object[]> RegisteredCases =>
        LciConformanceCorpus.Registrations.Select(
            (test, index) => new { Test = test, Index = index })
        .Select(item => new object[] { item.Index, item.Test.Id });

    [Theory]
    [MemberData(nameof(RegisteredCases))]
    public async Task Matches_upstream_lci_result(int index, string id)
    {
        LciTestRegistration test = LciConformanceCorpus.Registrations[index];
        test.Id.Should().Be(id);

        string source = ReadUtf8PreservingBom(test.SourcePath);
        var syntaxTree = SyntaxTree.ParseText(source, test.SourcePath);
        var compilation = LolcodeCompilation.Create(syntaxTree);
        string assemblyPath = Path.Combine(_tempDirectory, "test.dll");
        EmitResult emitResult = compilation.Emit(assemblyPath, _runtimeAssemblyPath);

        if (!emitResult.Success)
        {
            if (test.ExpectError)
                return;

            Assert.Fail(
                $"Compilation failed:\n{string.Join(Environment.NewLine, emitResult.Diagnostics)}");
        }

        ProcessResult processResult = await RunAsync(
            emitResult.OutputPath!,
            test.InputPath is null
                ? null
                : ReadUtf8PreservingBom(test.InputPath),
            test.WorkingDirectoryPath ?? _tempDirectory);

        if (test.ExpectError)
        {
            processResult.ExitCode.Should().NotBe(
                0,
                $"upstream marks {test.Id} with ADD_LOL_TEST(... ERROR)");
            return;
        }

        processResult.ExitCode.Should().Be(
            0,
            $"stderr was:{Environment.NewLine}{processResult.StandardError}");
        string expectedOutput = ReadUtf8PreservingBom(test.ExpectedOutputPath!);
        processResult.StandardOutput.Should().Be(expectedOutput);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
        catch
        {
            // Test cleanup is best effort.
        }
    }

    private static string ReadUtf8PreservingBom(string path)
        => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
            .GetString(File.ReadAllBytes(path));

    private static async Task<ProcessResult> RunAsync(
        string assemblyPath,
        string? standardInput,
        string workingDirectory)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        TextReader originalInput = Console.In;
        TextWriter originalOutput = Console.Out;
        TextWriter originalError = Console.Error;
        string originalWorkingDirectory = Environment.CurrentDirectory;
        var loadContext = new LciAssemblyLoadContext();
        int exitCode = 0;

        try
        {
            Console.SetIn(new StringReader(standardInput ?? string.Empty));
            Console.SetOut(output);
            Console.SetError(error);
            Environment.CurrentDirectory = workingDirectory;

            Assembly assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
            MethodInfo entryPoint = assembly.EntryPoint
                ?? throw new InvalidOperationException("Emitted assembly has no entry point.");
            object?[]? arguments = entryPoint.GetParameters().Length == 0
                ? null
                : [Array.Empty<string>()];

            try
            {
                object? result = entryPoint.Invoke(null, arguments);
                if (result is Task task)
                    await task;
            }
            catch (TargetInvocationException ex)
            {
                exitCode = 1;
                error.Write(ex.InnerException ?? ex);
            }
        }
        finally
        {
            Console.SetIn(originalInput);
            Console.SetOut(originalOutput);
            Console.SetError(originalError);
            Environment.CurrentDirectory = originalWorkingDirectory;
            loadContext.Unload();
        }

        return new ProcessResult(exitCode, output.ToString(), error.ToString());
    }

    private sealed record ProcessResult(
        int ExitCode,
        string StandardOutput,
        string StandardError);

    private sealed class LciAssemblyLoadContext() : AssemblyLoadContext(isCollectible: true)
    {
        protected override Assembly? Load(AssemblyName assemblyName)
            => Default.Assemblies.SingleOrDefault(
                assembly => AssemblyName.ReferenceMatchesDefinition(
                    assembly.GetName(), assemblyName));
    }
}

[CollectionDefinition(nameof(LciConformanceCollection), DisableParallelization = true)]
public sealed class LciConformanceCollection
{
}
