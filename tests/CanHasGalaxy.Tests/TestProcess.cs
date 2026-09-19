using System.Diagnostics;

namespace CanHasGalaxy.Tests;

internal static class TestProcess
{
    private static readonly string Configuration =
        new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;

    internal static string RepoRoot
    {
        get
        {
            string? directory = AppContext.BaseDirectory;
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory, "dotnet-lolcode.slnx")))
                    return directory;

                directory = Directory.GetParent(directory)?.FullName;
            }

            throw new InvalidOperationException("Could not locate dotnet-lolcode.slnx.");
        }
    }

    internal static string CreateTemporaryDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "can-has-galaxy-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    internal static string AssemblyPath(string project, string assemblyName) =>
        Path.Combine(RepoRoot, "samples", "project-based", "can-has-galaxy", project,
            "bin", Configuration, "net10.0", $"{assemblyName}.dll");

    internal static (int ExitCode, string StdOut, string StdErr) Run(
        string assemblyPath,
        string workingDirectory,
        string input)
    {
        var startInfo = new ProcessStartInfo("dotnet", $"\"{assemblyPath}\"")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using Process process = Process.Start(startInfo)!;
        process.StandardInput.Write(input);
        process.StandardInput.Close();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output, error);
    }
}
