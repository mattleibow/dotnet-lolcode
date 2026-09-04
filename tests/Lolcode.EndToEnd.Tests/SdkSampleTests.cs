using System.Diagnostics;

namespace Lolcode.EndToEnd.Tests;

/// <summary>
/// Integration tests that run file-based and project-based samples using the
/// LOLCODE SDK.
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

    private static (int ExitCode, string StdOut, string StdErr) RunDotnet(
        string args,
        string workingDir,
        string? standardInput = null,
        int timeoutMs = 30_000)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = args,
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = standardInput is not null,
        };

        using var process = Process.Start(psi)!;

        if (standardInput is not null)
        {
            process.StandardInput.Write(standardInput);
            process.StandardInput.Close();
        }
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit(timeoutMs);

        return (process.ExitCode, stdout, stderr);
    }

    /// <summary>
    /// Discovers the primary file-based sample catalog.
    /// </summary>
    public static IEnumerable<object[]> GetFileBasedSamples()
    {
        string samplesDir = Path.Combine(RepoRoot, "samples");
        foreach (string sourceFile in Directory.EnumerateFiles(samplesDir, "*.lol", SearchOption.AllDirectories)
            .Where(path => !path.StartsWith(Path.Combine(samplesDir, "project-based"), StringComparison.Ordinal))
            .Order())
        {
            string relativePath = Path.GetRelativePath(RepoRoot, sourceFile);
            yield return [relativePath];
        }
    }

    [Theory]
    [MemberData(nameof(GetFileBasedSamples))]
    public void FileBasedSample_Runs(string sampleFile)
    {
        var fullPath = Path.Combine(RepoRoot, sampleFile);
        var standardInput = Path.GetFileName(fullPath) switch
        {
            "guess.lol" => "42\n",
            "adventure.lol" => "quit\n",
            "Game.lol" => "TESTER\nflee\n",
            "chess.lol" => "quit\n",
            "calculator.lol" => "quit\n",
            _ => null,
        };
        var (exitCode, stdout, stderr) = RunDotnet(
            $"run --file \"{Path.GetFileName(fullPath)}\"",
            Path.GetDirectoryName(fullPath)!,
            standardInput,
            timeoutMs: 60_000);

        exitCode.Should().Be(0, $"dotnet run --file failed for {sampleFile}:\n{stderr}\n{stdout}");
    }

    [Fact]
    public void ProjectBasedSample_Runs_CorrectOutput()
    {
        var projectFiles = Directory
            .EnumerateFiles(Path.Combine(RepoRoot, "samples"), "*.lolproj", SearchOption.AllDirectories)
            .ToArray();
        projectFiles.Should().ContainSingle("only the dedicated project-based sample should use a .lolproj");

        var (exitCode, stdout, stderr) = RunDotnet(
            $"run --project \"{projectFiles[0]}\"",
            RepoRoot);

        exitCode.Should().Be(0, $"dotnet run --project failed:\n{stderr}");

        var output = stdout.Replace("\r\n", "\n").TrimEnd('\n');
        output.Should().Be("HAI WORLD FROM A LOLPROJ!");
    }

    [Fact]
    public void FileBasedHelloWorld_BuildsWithoutRunning()
    {
        var sampleDir = Path.Combine(RepoRoot, "samples", "basics", "hello-world");
        var (exitCode, stdout, stderr) = RunDotnet("build hello.lol", sampleDir);

        exitCode.Should().Be(0, $"dotnet build failed:\n{stderr}\n{stdout}");
    }

    [Fact]
    public void FileBasedHelloWorld_Runs_CorrectOutput()
    {
        var sampleDir = Path.Combine(RepoRoot, "samples", "basics", "hello-world");
        var localBuildTasksDir = Path.Combine(RepoRoot, "src", "Lolcode.Build", "bin", "Debug", "net10.0");
        var (exitCode, stdout, stderr) = RunDotnet("run --file hello.lol --verbosity diagnostic", sampleDir);

        exitCode.Should().Be(0, $"dotnet run --file failed:\n{stderr}");
        stdout.Should().Contain(localBuildTasksDir, "file-based samples should use the source-built compiler");

        var output = stdout.Replace("\r\n", "\n").TrimEnd('\n');
        output.Should().EndWith("HAI WORLD!");
    }

    [Fact]
    public void FileBasedHelloWorld_ReportsMissingLocalCompiler()
    {
        var sampleDir = Path.Combine(RepoRoot, "samples", "basics", "hello-world");
        var (exitCode, stdout, stderr) = RunDotnet(
            "run --file hello.lol --configuration MissingLocalCompiler",
            sampleDir);

        exitCode.Should().NotBe(0);
        $"{stdout}\n{stderr}".Should().Contain(
            "The source-built LOLCODE compiler was not found",
            "file-based samples must never fall back to the packaged compiler");
    }

    [Fact]
    public void Chess_Runs_PlayerAndAiTurn()
    {
        var sampleDir = Path.Combine(RepoRoot, "samples", "games", "chess");
        var (exitCode, stdout, stderr) = RunDotnet(
            "run --file chess.lol",
            sampleDir,
            "e2\ne4\nquit\n",
            timeoutMs: 60_000);

        exitCode.Should().Be(0, $"dotnet run --file failed:\n{stderr}");
        stdout.Should().Contain("8 | r n b q k b n r | 8");
        stdout.Should().Contain("1 | R N B Q K B N R | 1");
        stdout.Should().Contain("AI MOVE:");
        stdout.Should().Contain("4 | . . . . P . . . | 4");
        stdout.Should().Contain("KTHXBAI! TANKS 4 PLAYIN LOLCHESS!");
    }
}
