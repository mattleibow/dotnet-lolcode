using System.Diagnostics;
using Lolcode.CodeAnalysis;
using Lolcode.CodeAnalysis.Text;

namespace Lolcode.CodeAnalysis.Tests;

/// <summary>
/// Runs all .lol/.txt conformance test pairs from the tests/ directory.
/// Each .lol file is compiled and run; stdout is compared against the .txt file.
/// </summary>
public class ConformanceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _runtimeDll;

    public ConformanceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "lolcode-conformance", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        string testDir = AppContext.BaseDirectory;
        _runtimeDll = Path.Combine(testDir, "Lolcode.Runtime.dll");
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
    }

    /// <summary>
    /// Discovers all test pairs from the tests/ directory.
    /// </summary>
    public static IEnumerable<object[]> GetTestPairs()
    {
        // Walk up from test output dir to find repo root
        string? repoRoot = FindRepoRoot();
        if (repoRoot == null)
            yield break;

        string testsDir = Path.Combine(repoRoot, "tests");
        if (!Directory.Exists(testsDir))
            yield break;

        foreach (string lolFile in Directory.EnumerateFiles(testsDir, "*.lol", SearchOption.AllDirectories))
        {
            string txtFile = Path.ChangeExtension(lolFile, ".txt");
            if (File.Exists(txtFile))
            {
                // Get relative path for display
                string relativePath = Path.GetRelativePath(testsDir, lolFile);
                yield return [relativePath, lolFile, txtFile];
            }
        }
    }

    private static string? FindRepoRoot()
    {
        string dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir, ".git")))
                return dir;
            string? parent = Path.GetDirectoryName(dir);
            if (parent == dir) break;
            dir = parent!;
        }
        return null;
    }

    [Theory]
    [MemberData(nameof(GetTestPairs))]
    public void ConformanceTest(string testName, string lolFile, string txtFile)
    {
        string source = File.ReadAllText(lolFile);
        string expectedOutput = File.ReadAllText(txtFile)
            .Replace("\r\n", "\n")
            .TrimEnd('\n');

        var sourceText = SourceText.From(source, lolFile);
        string outputPath = Path.Combine(_tempDir, $"test_{Guid.NewGuid():N}.dll");
        var result = Compilation.Compile(sourceText, outputPath, _runtimeDll);

        if (!result.Success)
        {
            var errors = string.Join("\n", result.Diagnostics.Select(d => d.ToString()));
            Assert.Fail($"Compilation of {testName} failed:\n{errors}");
            return;
        }

        // Copy runtime next to output
        string runtimeDest = Path.Combine(Path.GetDirectoryName(result.OutputPath!)!, "Lolcode.Runtime.dll");
        if (!File.Exists(runtimeDest))
            File.Copy(_runtimeDll, runtimeDest, overwrite: true);

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = result.OutputPath!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var process = Process.Start(psi)!;
        string actual = process.StandardOutput.ReadToEnd()
            .Replace("\r\n", "\n")
            .TrimEnd('\n');
        process.WaitForExit(10000);

        actual.Should().Be(expectedOutput, because: $"test {testName} output should match expected");
    }
}
