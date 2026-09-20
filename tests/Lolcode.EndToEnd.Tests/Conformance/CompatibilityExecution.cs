using System.Diagnostics;
using System.Text;
using Lolcode.CodeAnalysis;
using Lolcode.CodeAnalysis.Syntax;

namespace Lolcode.EndToEnd.Tests;

internal sealed record ProcessExecution(
    int ExitCode,
    byte[] StandardOutput,
    byte[] StandardError)
{
    internal string StandardOutputText => Encoding.UTF8.GetString(StandardOutput);

    internal string StandardErrorText => Encoding.UTF8.GetString(StandardError);
}

/// <summary>Runs fixture programs as isolated child processes with bounded output capture.</summary>
internal static class CompatibilityProcessRunner
{
    private const int MaximumCapturedBytes = 4 * 1024 * 1024;

    internal static async Task<ProcessExecution> RunAsync(
        ProcessStartInfo startInfo,
        string? standardInput,
        TimeSpan timeout)
    {
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.RedirectStandardInput = standardInput is not null;
        startInfo.Environment["DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER"] = "1";

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start '{startInfo.FileName}'.");
        Task<byte[]> outputTask = ReadAllBytesAsync(process.StandardOutput.BaseStream, "stdout");
        Task<byte[]> errorTask = ReadAllBytesAsync(process.StandardError.BaseStream, "stderr");
        Task inputTask = WriteInputAsync(process, standardInput);

        using var cancellation = new CancellationTokenSource(timeout);
        try
        {
            Task exitTask = process.WaitForExitAsync(cancellation.Token);
            while (!exitTask.IsCompleted)
            {
                var waiters = new List<Task> { exitTask };
                if (!outputTask.IsCompleted)
                    waiters.Add(outputTask);
                if (!errorTask.IsCompleted)
                    waiters.Add(errorTask);

                Task first = await Task.WhenAny(waiters);
                if (first == outputTask)
                    await outputTask;
                if (first == errorTask)
                    await errorTask;
            }

            await exitTask; // Propagates timeout cancellation after the loop condition.
        }
        catch (OperationCanceledException)
        {
            await StopAndDrainAsync(process, outputTask, errorTask, inputTask);
            throw new TimeoutException(
                $"'{startInfo.FileName}' did not exit within {timeout.TotalSeconds:0} seconds.");
        }
        catch (OutputLimitExceededException exception)
        {
            await StopAndDrainAsync(process, outputTask, errorTask, inputTask);
            throw new InvalidOperationException(
                $"'{startInfo.FileName}' exceeded the {MaximumCapturedBytes:N0}-byte " +
                $"{exception.StreamName} capture limit.",
                exception);
        }

        await Task.WhenAll(outputTask, errorTask, inputTask);
        return new ProcessExecution(process.ExitCode, outputTask.Result, errorTask.Result);
    }

    private static async Task WriteInputAsync(Process process, string? standardInput)
    {
        if (standardInput is null)
            return;

        await process.StandardInput.WriteAsync(standardInput);
        process.StandardInput.Close();
    }

    private static async Task StopAndDrainAsync(
        Process process,
        Task<byte[]> outputTask,
        Task<byte[]> errorTask,
        Task inputTask)
    {
        if (!process.HasExited)
            process.Kill(entireProcessTree: true);
        await process.WaitForExitAsync();
        try
        {
            await Task.WhenAll(outputTask, errorTask, inputTask);
        }
        catch
        {
            // The primary timeout/size exception is more useful than a broken input pipe.
        }
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream, string streamName)
    {
        using var result = new MemoryStream();
        byte[] buffer = new byte[81920];
        while (true)
        {
            int bytesRead = await stream.ReadAsync(buffer);
            if (bytesRead == 0)
                break;

            if (result.Length + bytesRead > MaximumCapturedBytes)
                throw new OutputLimitExceededException(streamName);
            await result.WriteAsync(buffer.AsMemory(0, bytesRead));
        }
        return result.ToArray();
    }

    private sealed class OutputLimitExceededException(string streamName) : Exception
    {
        internal string StreamName { get; } = streamName;
    }
}

/// <summary>Compiles a fixture with dotnet-lolcode and executes its emitted assembly.</summary>
internal sealed class DotNetLolcodeEngine : IDisposable
{
    private static readonly string[] ProviderAssemblyNames =
    [
        "Lolcode.Runtime.String.dll",
        "Lolcode.Runtime.Stdlib.dll",
        "Lolcode.Runtime.Stdio.dll",
        "Lolcode.Runtime.Socks.dll",
    ];

    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "lolcode-compatibility",
        Guid.NewGuid().ToString("N"));
    private readonly string _runtimeAssemblyPath =
        Path.Combine(AppContext.BaseDirectory, "Lolcode.Runtime.dll");

    internal DotNetLolcodeEngine() => Directory.CreateDirectory(_directory);

    internal async Task<ProcessExecution> RunAsync(LciTestRegistration test)
    {
        string source = ReadUtf8(test.SourcePath);
        EmitResult result = LolcodeCompilation.Create(SyntaxTree.ParseText(source, test.SourcePath))
            .Emit(Path.Combine(_directory, $"{Guid.NewGuid():N}.dll"), _runtimeAssemblyPath);

        if (!result.Success)
        {
            string diagnostics = string.Join(Environment.NewLine, result.Diagnostics);
            return new ProcessExecution(1, [], Encoding.UTF8.GetBytes(diagnostics));
        }

        string assemblyPath = result.OutputPath
            ?? throw new InvalidOperationException("A successful emit did not produce an assembly path.");
        CopyRuntimeDependencies(Path.GetDirectoryName(assemblyPath)!);
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = test.WorkingDirectoryPath ?? _directory,
        };
        startInfo.ArgumentList.Add(assemblyPath);
        return await CompatibilityProcessRunner.RunAsync(
            startInfo,
            test.InputPath is null ? null : ReadUtf8(test.InputPath),
            TimeSpan.FromSeconds(30));
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch
        {
            // Process output files are best-effort test cleanup.
        }
    }

    private void CopyRuntimeDependencies(string targetDirectory)
    {
        Copy(_runtimeAssemblyPath, targetDirectory);
        foreach (string providerAssemblyName in ProviderAssemblyNames)
        {
            string provider = Path.Combine(AppContext.BaseDirectory, providerAssemblyName);
            if (File.Exists(provider))
                Copy(provider, targetDirectory);
        }
    }

    private static void Copy(string source, string targetDirectory) =>
        File.Copy(source, Path.Combine(targetDirectory, Path.GetFileName(source)), overwrite: true);

    internal static string ReadUtf8(string path) =>
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
            .GetString(File.ReadAllBytes(path));
}
