using System.Diagnostics;

namespace Lolcode.CodeAnalysis.Tests;

/// <summary>
/// Integration tests that build and run sample projects using the LOLCODE SDK.
/// These tests verify the MSBuild integration works end-to-end by invoking
/// <c>dotnet build</c> and <c>dotnet run</c> on real <c>.lolproj</c> files.
/// </summary>
public class SdkSampleTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    private static string FindRepoRoot()
    {
        string dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "dotnet-lolcode.slnx")))
                return dir;
            dir = Path.GetDirectoryName(dir)!;
        }
        throw new InvalidOperationException("Could not find repo root (looked for dotnet-lolcode.slnx)");
    }

    private static (int ExitCode, string StdOut, string StdErr) RunDotnet(string args, string workingDir, int timeoutMs = 30_000)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = args,
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var process = Process.Start(psi)!;
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit(timeoutMs);

        return (process.ExitCode, stdout, stderr);
    }

    [Fact]
    public void SdkHelloWorld_Builds()
    {
        var sampleDir = Path.Combine(RepoRoot, "samples", "sdk-hello-world");
        var (exitCode, stdout, stderr) = RunDotnet("build", sampleDir);

        exitCode.Should().Be(0, $"dotnet build failed:\n{stderr}");
    }

    [Fact]
    public void SdkHelloWorld_Runs_CorrectOutput()
    {
        var sampleDir = Path.Combine(RepoRoot, "samples", "sdk-hello-world");
        var (exitCode, stdout, stderr) = RunDotnet("run", sampleDir);

        exitCode.Should().Be(0, $"dotnet run failed:\n{stderr}");

        var output = stdout.Replace("\r\n", "\n").TrimEnd('\n');
        output.Should().Be("HAI WORLD!\nI CAN HAS .NET LOLCODE SDK!");
    }

    [Fact]
    public void SdkArenaGame_Builds()
    {
        var sampleDir = Path.Combine(RepoRoot, "samples", "sdk-arena-game");
        var (exitCode, stdout, stderr) = RunDotnet("build", sampleDir);

        exitCode.Should().Be(0, $"dotnet build failed:\n{stderr}");
    }
}
