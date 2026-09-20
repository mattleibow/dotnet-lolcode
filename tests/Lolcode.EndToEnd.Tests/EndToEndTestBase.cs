using System.Diagnostics;
using System.Text;
using Lolcode.CodeAnalysis;
using Lolcode.CodeAnalysis.Syntax;
using Lolcode.CodeAnalysis.Text;

namespace Lolcode.EndToEnd.Tests;

/// <summary>
/// Base class for end-to-end LOLCODE tests. Provides helpers to compile source,
/// run the resulting DLL, and assert on stdout or diagnostics.
/// </summary>
public abstract class EndToEndTestBase : IDisposable
{
    private const int DefaultProgramTimeoutSeconds = 60;
    private static readonly string[] ProviderAssemblyNames =
    [
        "Lolcode.Runtime.String.dll",
        "Lolcode.Runtime.Stdlib.dll",
        "Lolcode.Runtime.Stdio.dll",
        "Lolcode.Runtime.Socks.dll",
    ];
    private readonly string _tempDir;
    private readonly string _runtimeDll;
    private readonly string _runtimeDirectory;

    /// <summary>Gets the isolated directory used by the current test.</summary>
    protected string TestDirectory => _tempDir;

    /// <summary>Loads a canonical compatibility fixture copied beside the test assembly.</summary>
    protected static string FixtureSource(string relativePath)
        => CompatibilityFixtures.ReadSource(relativePath);

    /// <summary>Captured output and process status from an emitted program.</summary>
    protected sealed record ExecutionResult(
        int ExitCode,
        string StandardOutput,
        string StandardError,
        byte[] StandardOutputBytes,
        byte[] StandardErrorBytes);

    protected EndToEndTestBase()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "lolcode-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        string testDir = AppContext.BaseDirectory;
        _runtimeDirectory = testDir;
        _runtimeDll = Path.Combine(testDir, "Lolcode.Runtime.dll");
        if (!File.Exists(_runtimeDll))
            throw new FileNotFoundException($"Runtime DLL not found at: {_runtimeDll}");
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
    }

    /// <summary>Compiles and runs LOLCODE source, returns stdout.</summary>
    protected string CompileAndRun(string source, string? stdin = null)
    {
        ExecutionResult result = CompileAndRunWithResult(source, stdin);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Runtime error (exit code {result.ExitCode}):\n{result.StandardError}");
        }
        return result.StandardOutput;
    }

    /// <summary>Compiles and runs LOLCODE source, capturing stdout and stderr.</summary>
    protected ExecutionResult CompileAndRunWithResult(
        string source,
        string? stdin = null,
        string? workingDirectory = null)
    {
        string assemblyPath = CompileToAssembly(source);

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = assemblyPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = stdin != null,
            WorkingDirectory = workingDirectory ?? _tempDir,
        };

        using var process = Process.Start(psi)!;

        if (stdin != null)
        {
            process.StandardInput.Write(stdin);
            process.StandardInput.Close();
        }

        Task<byte[]> output = ReadAllBytesAsync(process.StandardOutput.BaseStream);
        Task<byte[]> error = ReadAllBytesAsync(process.StandardError.BaseStream);
        TimeSpan timeout = GetProgramTimeout();
        if (!process.WaitForExit(timeout))
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
            Task.WaitAll(output, error);
            throw new TimeoutException(
                $"LOLCODE program did not exit within {timeout.TotalSeconds:0} seconds.");
        }
        Task.WaitAll(output, error);
        return new ExecutionResult(
            process.ExitCode,
            Encoding.UTF8.GetString(output.Result),
            Encoding.UTF8.GetString(error.Result),
            output.Result,
            error.Result);
    }

    /// <summary>Compiles LOLCODE source into the isolated test directory.</summary>
    protected string CompileToAssembly(string source, string sourcePath = "test.lol")
    {
        var tree = SyntaxTree.ParseText(source, sourcePath);
        string outputPath = Path.Combine(_tempDir, $"test_{Guid.NewGuid():N}.dll");
        var compilation = LolcodeCompilation.Create(tree);
        var result = compilation.Emit(outputPath, _runtimeDll);

        if (!result.Success)
        {
            var errors = string.Join("\n", result.Diagnostics.Select(d => d.ToString()));
            throw new InvalidOperationException($"Compilation failed:\n{errors}");
        }

        CopyRuntimeDependencies(Path.GetDirectoryName(result.OutputPath!)!);
        return result.OutputPath!;
    }

    /// <summary>Compiles and runs source, asserts stdout matches expected output.</summary>
    protected void AssertOutput(string source, string expectedOutput, string? stdin = null)
    {
        string actual = CompileAndRun(source, stdin);
        actual = actual.Replace("\r\n", "\n").TrimEnd('\n');
        expectedOutput = expectedOutput.Replace("\r\n", "\n").TrimEnd('\n');
        actual.Should().Be(expectedOutput);
    }

    /// <summary>Asserts that running the source produces a runtime error containing the substring.</summary>
    protected void AssertRuntimeError(
        string source,
        string expectedErrorSubstring,
        string? expectedOutput = null)
    {
        var tree = SyntaxTree.ParseText(source, "test.lol");
        string outputPath = Path.Combine(_tempDir, $"test_{Guid.NewGuid():N}.dll");
        var compilation = LolcodeCompilation.Create(tree);
        var result = compilation.Emit(outputPath, _runtimeDll);

        if (!result.Success)
        {
            var errors = string.Join("\n", result.Diagnostics.Select(d => d.ToString()));
            throw new InvalidOperationException($"Compilation failed (expected runtime error instead):\n{errors}");
        }

        CopyRuntimeDependencies(Path.GetDirectoryName(result.OutputPath!)!);

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = result.OutputPath!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var process = Process.Start(psi)!;
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();
        TimeSpan timeout = GetProgramTimeout();
        if (!process.WaitForExit(timeout))
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
            Task.WaitAll(output, error);
            throw new TimeoutException(
                $"LOLCODE program did not exit within {timeout.TotalSeconds:0} seconds.");
        }
        Task.WaitAll(output, error);

        process.ExitCode.Should().NotBe(0, "Expected a runtime error");
        error.Result.Should().Contain(expectedErrorSubstring);
        if (expectedOutput is not null)
            output.Result.Replace("\r\n", "\n").TrimEnd('\n').Should().Be(expectedOutput);
    }

    /// <summary>Asserts that compiling the source produces a diagnostic with the given ID.</summary>
    protected void AssertCompileError(string source, string expectedDiagnosticId)
    {
        var tree = SyntaxTree.ParseText(source, "test.lol");
        var compilation = LolcodeCompilation.Create(tree);
        var diagnostics = compilation.GetDiagnostics();

        diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error).Should().BeTrue("Expected compilation to fail");
        diagnostics.Should().Contain(d => d.Id.Contains(expectedDiagnosticId));
    }

    private static TimeSpan GetProgramTimeout()
    {
        const string variable = "LOLCODE_TEST_TIMEOUT_SECONDS";
        string? configured = Environment.GetEnvironmentVariable(variable);
        if (configured is not null &&
            int.TryParse(configured, out int seconds) &&
            seconds > 0)
        {
            return TimeSpan.FromSeconds(seconds);
        }
        return TimeSpan.FromSeconds(DefaultProgramTimeoutSeconds);
    }

    private void CopyRuntimeDependencies(string outputDirectory)
    {
        File.Copy(
            _runtimeDll,
            Path.Combine(outputDirectory, "Lolcode.Runtime.dll"),
            overwrite: true);
        foreach (string providerAssemblyName in ProviderAssemblyNames)
        {
            string source = Path.Combine(_runtimeDirectory, providerAssemblyName);
            if (File.Exists(source))
            {
                File.Copy(
                    source,
                    Path.Combine(outputDirectory, providerAssemblyName),
                    overwrite: true);
            }
        }

    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream)
    {
        using var result = new MemoryStream();
        await stream.CopyToAsync(result);
        return result.ToArray();
    }
}

internal static class CompatibilityFixtures
{
    internal static string ReadSource(string relativePath)
    {
        string path = Path.Combine(
            AppContext.BaseDirectory,
            "Compatibility",
            relativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.ReadAllText(path, Encoding.UTF8);
    }
}
