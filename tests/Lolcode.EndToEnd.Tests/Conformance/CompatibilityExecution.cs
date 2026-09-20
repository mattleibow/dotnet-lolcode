using System.Diagnostics;
using System.Runtime.InteropServices;
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
    private static readonly TimeSpan CleanupTimeout = TimeSpan.FromSeconds(1);

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
        ConfigureUnixProcessGroup(startInfo);

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start '{startInfo.FileName}'.");
        using var cancellation = new CancellationTokenSource();
        Task<byte[]> outputTask = ReadAllBytesAsync(
            process.StandardOutput.BaseStream, "stdout", cancellation.Token);
        Task<byte[]> errorTask = ReadAllBytesAsync(
            process.StandardError.BaseStream, "stderr", cancellation.Token);
        Task inputTask = WriteInputAsync(process, standardInput, cancellation.Token);
        Task exitTask = process.WaitForExitAsync(cancellation.Token);
        Task deadlineTask = Task.Delay(timeout);
        var pending = new List<Task> { outputTask, errorTask, inputTask, exitTask };

        try
        {
            while (pending.Count > 0)
            {
                Task completed = await Task.WhenAny(pending.Append(deadlineTask));
                if (completed == deadlineTask)
                    throw new TimeoutException();

                await completed;
                pending.Remove(completed);
            }

            return new ProcessExecution(process.ExitCode, outputTask.Result, errorTask.Result);
        }
        catch (TimeoutException)
        {
            await StopAndDrainAsync(
                process, cancellation, outputTask, errorTask, inputTask);
            throw new TimeoutException(
                $"'{startInfo.FileName}' did not complete within {timeout.TotalSeconds:0} seconds.");
        }
        catch (OutputLimitExceededException exception)
        {
            await StopAndDrainAsync(
                process, cancellation, outputTask, errorTask, inputTask);
            throw new InvalidOperationException(
                $"'{startInfo.FileName}' exceeded the {MaximumCapturedBytes:N0}-byte " +
                $"{exception.StreamName} capture limit.",
                exception);
        }
        catch
        {
            await StopAndDrainAsync(
                process, cancellation, outputTask, errorTask, inputTask);
            throw;
        }
    }

    private static async Task WriteInputAsync(
        Process process,
        string? standardInput,
        CancellationToken cancellationToken)
    {
        if (standardInput is null)
            return;

        await process.StandardInput.WriteAsync(standardInput.AsMemory(), cancellationToken);
        process.StandardInput.Close();
    }

    private static async Task StopAndDrainAsync(
        Process process,
        CancellationTokenSource cancellation,
        Task<byte[]> outputTask,
        Task<byte[]> errorTask,
        Task inputTask)
    {
        cancellation.Cancel();
        UnixProcessGroups.TryKill(process.Id);
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // The direct process can exit while descendants still retain its pipes.
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // A process that has already been reaped cannot be killed again.
        }

        try
        {
            process.StandardInput.Close();
            process.StandardOutput.Close();
            process.StandardError.Close();
        }
        catch
        {
            // Closing a pipe is best effort while a concurrent I/O operation is being cancelled.
        }

        Task drains = Task.WhenAll(outputTask, errorTask, inputTask);
        Task exit = process.WaitForExitAsync();
        Task cleanup = Task.WhenAll(IgnoreFailuresAsync(drains), IgnoreFailuresAsync(exit));
        _ = await Task.WhenAny(cleanup, Task.Delay(CleanupTimeout));
    }

    private static async Task IgnoreFailuresAsync(Task task)
    {
        try
        {
            await task;
        }
        catch
        {
            // The original timeout, output limit, or process exception is more useful.
        }
    }

    private static async Task<byte[]> ReadAllBytesAsync(
        Stream stream,
        string streamName,
        CancellationToken cancellationToken)
    {
        using var result = new MemoryStream();
        byte[] buffer = new byte[81920];
        while (true)
        {
            int bytesRead = await stream.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0)
                break;

            if (result.Length + bytesRead > MaximumCapturedBytes)
                throw new OutputLimitExceededException(streamName);
            await result.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        }
        return result.ToArray();
    }

    private sealed class OutputLimitExceededException(string streamName) : Exception
    {
        internal string StreamName { get; } = streamName;
    }

    private static void ConfigureUnixProcessGroup(ProcessStartInfo startInfo)
    {
        if (OperatingSystem.IsWindows())
            return;

        string executable = startInfo.FileName;
        string[] arguments = startInfo.ArgumentList.ToArray();
        startInfo.FileName = "python3";
        startInfo.ArgumentList.Clear();
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add(
            """
            import os
            import signal
            import sys

            os.setsid()
            process_group = os.getpgrp()
            child = os.fork()
            if child == 0:
                try:
                    os.execvpe(sys.argv[1], sys.argv[1:], os.environ)
                except OSError as error:
                    os.write(2, f"{error}\n".encode())
                    os._exit(127)

            _, status = os.waitpid(child, 0)
            read_pipe, write_pipe = os.pipe()
            helper = os.fork()
            if helper == 0:
                os.close(write_pipe)
                os.read(read_pipe, 1)
                os.close(read_pipe)
                os.setpgrp()
                os.killpg(process_group, signal.SIGKILL)
                os._exit(0)
            os.close(read_pipe)
            os.close(write_pipe)
            exit_code = os.waitstatus_to_exitcode(status)
            os._exit(exit_code if exit_code >= 0 else 128 - exit_code)
            """);
        startInfo.ArgumentList.Add(executable);
        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(argument);
    }

    private static class UnixProcessGroups
    {
        private const int SigKill = 9;

        internal static void TryKill(int processId)
        {
            if (OperatingSystem.IsWindows())
                return;

            _ = kill(-processId, SigKill);
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int kill(int pid, int signal);
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
