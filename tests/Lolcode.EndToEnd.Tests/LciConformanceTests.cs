using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Lolcode.CodeAnalysis;
using Lolcode.CodeAnalysis.Syntax;
using Xunit.Sdk;

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

    public static IEnumerable<object[]> PassingCases =>
        LciConformanceCorpus.Cases.Select(
            (test, index) => new { Test = test, Index = index })
        .Where(item => item.Test.Classification.Status == "pass")
        .Select(item => new object[] { item.Index, item.Test.Id });

    [Theory]
    [MemberData(nameof(PassingCases))]
    public async Task Matches_upstream_lci_result(int index, string id)
    {
        LciConformanceCase test = LciConformanceCorpus.Cases[index];
        test.Id.Should().Be(id);

        LciTestRegistration registration = test.Registration;
        string source = ReadUtf8PreservingBom(registration.SourcePath);
        var syntaxTree = SyntaxTree.ParseText(source, registration.SourcePath);
        var compilation = LolcodeCompilation.Create(syntaxTree);
        string assemblyPath = Path.Combine(_tempDirectory, "test.dll");
        EmitResult emitResult = compilation.Emit(assemblyPath, _runtimeAssemblyPath);

        if (!emitResult.Success)
        {
            if (registration.ExpectError)
                return;

            Assert.Fail(
                $"Compilation failed:\n{string.Join(Environment.NewLine, emitResult.Diagnostics)}");
        }

        ProcessResult processResult = await RunAsync(
            emitResult.OutputPath!,
            registration.InputPath is null
                ? null
                : ReadUtf8PreservingBom(registration.InputPath),
            registration.WorkingDirectoryPath ?? _tempDirectory);

        if (registration.ExpectError)
        {
            processResult.ExitCode.Should().NotBe(
                0,
                $"upstream marks {registration.Id} with ADD_LOL_TEST(... ERROR)");
            return;
        }

        processResult.ExitCode.Should().Be(
            0,
            $"stderr was:{Environment.NewLine}{processResult.StandardError}");
        string expectedOutput = ReadUtf8PreservingBom(registration.ExpectedOutputPath!);
        processResult.StandardOutput.Should().Be(expectedOutput);
    }

    [Theory]
    [LciSkippedCases]
    public void Unsupported_lci_case(int index, string id, string feature)
    {
        _ = index;
        _ = id;
        _ = feature;
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

[AttributeUsage(AttributeTargets.Method)]
public sealed class LciSkippedCasesAttribute : DataAttribute
{
    public LciSkippedCasesAttribute()
    {
        Skip = "Unsupported feature; see Conformance/lci/status.json for the category and reason.";
    }

    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        => LciConformanceCorpus.Cases.Select(
                (test, index) => new { Test = test, Index = index })
            .Where(item => item.Test.Classification.Status == "skip")
            .Select(item => new object[]
            {
                item.Index,
                item.Test.Id,
                item.Test.Classification.Feature!,
            });
}
