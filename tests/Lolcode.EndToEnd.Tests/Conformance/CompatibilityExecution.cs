using System.Diagnostics;
using Microsoft.Win32.SafeHandles;
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
        if (OperatingSystem.IsWindows())
        {
            return await WindowsJobProcess.RunAsync(
                startInfo, standardInput, timeout, MaximumCapturedBytes, CleanupTimeout);
        }

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
            read_pipe, write_pipe = os.pipe()
            child = os.fork()
            if child == 0:
                try:
                    os.close(read_pipe)
                    os.close(write_pipe)
                    os.execvpe(sys.argv[1], sys.argv[1:], os.environ)
                except OSError as error:
                    os.write(2, f"{error}\n".encode())
                    os._exit(127)

            helper = os.fork()
            if helper == 0:
                os.close(write_pipe)
                os.read(read_pipe, 1)
                os.close(read_pipe)
                os.setpgrp()
                try:
                    os.killpg(process_group, signal.SIGKILL)
                except (ProcessLookupError, PermissionError):
                    pass
                os._exit(0)
            os.close(read_pipe)
            _, status = os.waitpid(child, 0)
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

/// <summary>
/// Windows implementation that assigns a suspended target to a kill-on-close job
/// before it can execute user code and create descendants.
/// </summary>
internal static class WindowsJobProcess
{
    private const uint CreateSuspended = 0x00000004;
    private const uint CreateUnicodeEnvironment = 0x00000400;
    private const uint StartfUseStdHandles = 0x00000100;
    private const uint HandleFlagInherit = 0x00000001;
    private const uint JobObjectLimitKillOnJobClose = 0x00002000;
    private const uint Infinite = 0xFFFFFFFF;
    private const uint WaitObject0 = 0;

    internal static async Task<ProcessExecution> RunAsync(
        ProcessStartInfo startInfo,
        string? standardInput,
        TimeSpan timeout,
        int maximumCapturedBytes,
        TimeSpan cleanupTimeout)
    {
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.RedirectStandardInput = standardInput is not null;
        startInfo.Environment["DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER"] = "1";

        using var process = Start(startInfo);
        using var cancellation = new CancellationTokenSource();
        Task<byte[]> outputTask = ReadAllBytesAsync(
            process.StandardOutput, "stdout", maximumCapturedBytes, cancellation.Token);
        Task<byte[]> errorTask = ReadAllBytesAsync(
            process.StandardError, "stderr", maximumCapturedBytes, cancellation.Token);
        Task inputTask = WriteInputAsync(process.StandardInput, standardInput, cancellation.Token);
        Task<int> exitTask = process.WaitForExitAndCloseJobAsync();
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

            return new ProcessExecution(exitTask.Result, outputTask.Result, errorTask.Result);
        }
        catch (TimeoutException)
        {
            await StopAndDrainAsync(process, cancellation, outputTask, errorTask, inputTask, cleanupTimeout);
            throw new TimeoutException(
                $"'{startInfo.FileName}' did not complete within {timeout.TotalSeconds:0} seconds.");
        }
        catch (OutputLimitExceededException exception)
        {
            await StopAndDrainAsync(process, cancellation, outputTask, errorTask, inputTask, cleanupTimeout);
            throw new InvalidOperationException(
                $"'{startInfo.FileName}' exceeded the {maximumCapturedBytes:N0}-byte " +
                $"{exception.StreamName} capture limit.",
                exception);
        }
        catch
        {
            await StopAndDrainAsync(process, cancellation, outputTask, errorTask, inputTask, cleanupTimeout);
            throw;
        }
    }

    private static WindowsOwnedProcess Start(ProcessStartInfo startInfo)
    {
        using SafeFileHandle childInput = CreatePipe(out SafeFileHandle parentInput);
        SafeFileHandle parentOutput = CreatePipe(out SafeFileHandle childOutput);
        SafeFileHandle parentError = CreatePipe(out SafeFileHandle childError);
        SafeFileHandle job = CreateKillOnCloseJob();
        using var environment = CreateEnvironmentBlock(startInfo.Environment);

        try
        {
            RemoveInheritance(parentInput);
            RemoveInheritance(parentOutput);
            RemoveInheritance(parentError);
            var startupInfo = new StartupInfo
            {
                cb = (uint)Marshal.SizeOf<StartupInfo>(),
                dwFlags = StartfUseStdHandles,
                hStdInput = childInput.DangerousGetHandle(),
                hStdOutput = childOutput.DangerousGetHandle(),
                hStdError = childError.DangerousGetHandle(),
            };
            string commandLine = BuildCommandLine(startInfo);
            if (!CreateProcessW(
                    null,
                    new StringBuilder(commandLine),
                    IntPtr.Zero,
                    IntPtr.Zero,
                    true,
                    CreateSuspended | CreateUnicodeEnvironment,
                    environment.DangerousGetHandle(),
                    string.IsNullOrEmpty(startInfo.WorkingDirectory) ? null : startInfo.WorkingDirectory,
                    ref startupInfo,
                    out ProcessInformation processInformation))
            {
                throw CreateWin32Exception("CreateProcessW");
            }

            SafeFileHandle processHandle = new(processInformation.hProcess, ownsHandle: true);
            using SafeFileHandle threadHandle = new(processInformation.hThread, ownsHandle: true);
            if (!AssignProcessToJobObject(job, processHandle))
            {
                _ = TerminateProcess(processHandle, 1);
                processHandle.Dispose();
                throw CreateWin32Exception("AssignProcessToJobObject");
            }

            if (ResumeThread(threadHandle) == uint.MaxValue)
            {
                _ = TerminateProcess(processHandle, 1);
                processHandle.Dispose();
                throw CreateWin32Exception("ResumeThread");
            }

            var ownedProcess = new WindowsOwnedProcess(
                processHandle,
                job,
                new FileStream(parentInput, FileAccess.Write, bufferSize: 4096, isAsync: true),
                new FileStream(parentOutput, FileAccess.Read, bufferSize: 81920, isAsync: true),
                new FileStream(parentError, FileAccess.Read, bufferSize: 81920, isAsync: true));
            childOutput.Dispose();
            childError.Dispose();
            return ownedProcess;
        }
        catch
        {
            job.Dispose();
            parentInput.Dispose();
            parentOutput.Dispose();
            parentError.Dispose();
            childOutput.Dispose();
            childError.Dispose();
            throw;
        }
    }

    private static async Task StopAndDrainAsync(
        WindowsOwnedProcess process,
        CancellationTokenSource cancellation,
        Task<byte[]> outputTask,
        Task<byte[]> errorTask,
        Task inputTask,
        TimeSpan cleanupTimeout)
    {
        cancellation.Cancel();
        process.Stop();
        Task drains = Task.WhenAll(outputTask, errorTask, inputTask);
        Task cleanup = Task.WhenAll(IgnoreFailuresAsync(drains), IgnoreFailuresAsync(process.WaitForExitAsync()));
        _ = await Task.WhenAny(cleanup, Task.Delay(cleanupTimeout));
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

    private static async Task WriteInputAsync(
        Stream stream,
        string? standardInput,
        CancellationToken cancellationToken)
    {
        if (standardInput is not null)
        {
            byte[] input = Encoding.UTF8.GetBytes(standardInput);
            await stream.WriteAsync(input, cancellationToken);
        }

        await stream.DisposeAsync();
    }

    private static async Task<byte[]> ReadAllBytesAsync(
        Stream stream,
        string streamName,
        int maximumCapturedBytes,
        CancellationToken cancellationToken)
    {
        await using var ownedStream = stream;
        using var result = new MemoryStream();
        byte[] buffer = new byte[81920];
        while (true)
        {
            int bytesRead = await stream.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0)
                break;

            if (result.Length + bytesRead > maximumCapturedBytes)
                throw new OutputLimitExceededException(streamName);
            await result.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        }

        return result.ToArray();
    }

    private static SafeFileHandle CreateKillOnCloseJob()
    {
        SafeFileHandle job = CreateJobObjectW(IntPtr.Zero, null);
        if (job.IsInvalid)
            throw CreateWin32Exception("CreateJobObjectW");

        var limits = new JobObjectExtendedLimitInformation
        {
            BasicLimitInformation = new JobObjectBasicLimitInformation
            {
                LimitFlags = JobObjectLimitKillOnJobClose,
            },
        };
        if (!SetInformationJobObject(
                job,
                JobObjectInfoClass.ExtendedLimitInformation,
                ref limits,
                (uint)Marshal.SizeOf<JobObjectExtendedLimitInformation>()))
        {
            job.Dispose();
            throw CreateWin32Exception("SetInformationJobObject");
        }

        return job;
    }

    private static SafeFileHandle CreatePipe(out SafeFileHandle otherEnd)
    {
        var attributes = new SecurityAttributes
        {
            nLength = Marshal.SizeOf<SecurityAttributes>(),
            bInheritHandle = true,
        };
        if (!CreatePipe(out IntPtr read, out IntPtr write, ref attributes, 0))
            throw CreateWin32Exception("CreatePipe");

        otherEnd = new SafeFileHandle(write, ownsHandle: true);
        return new SafeFileHandle(read, ownsHandle: true);
    }

    private static void RemoveInheritance(SafeFileHandle handle)
    {
        if (!SetHandleInformation(handle, HandleFlagInherit, 0))
            throw CreateWin32Exception("SetHandleInformation");
    }

    private static SafeBuffer CreateEnvironmentBlock(
        IDictionary<string, string?> environment)
    {
        string value = string.Concat(environment
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => $"{pair.Key}={pair.Value}\0")) + "\0";
        return new SafeHGlobalBuffer(Encoding.Unicode.GetBytes(value));
    }

    private static string BuildCommandLine(ProcessStartInfo startInfo)
    {
        var result = new StringBuilder(QuoteCommandLineArgument(startInfo.FileName));
        if (startInfo.ArgumentList.Count > 0)
        {
            foreach (string argument in startInfo.ArgumentList)
                result.Append(' ').Append(QuoteCommandLineArgument(argument));
        }
        else if (!string.IsNullOrWhiteSpace(startInfo.Arguments))
        {
            result.Append(' ').Append(startInfo.Arguments);
        }

        return result.ToString();
    }

    private static string QuoteCommandLineArgument(string argument)
    {
        if (argument.Length > 0 && argument.All(character => !char.IsWhiteSpace(character) && character != '"'))
            return argument;

        var quoted = new StringBuilder("\"");
        int backslashes = 0;
        foreach (char character in argument)
        {
            if (character == '\\')
            {
                backslashes++;
                continue;
            }

            if (character == '"')
                quoted.Append('\\', backslashes * 2 + 1).Append(character);
            else
                quoted.Append('\\', backslashes).Append(character);
            backslashes = 0;
        }

        quoted.Append('\\', backslashes * 2).Append('"');
        return quoted.ToString();
    }

    private static System.ComponentModel.Win32Exception CreateWin32Exception(string operation) =>
        new(Marshal.GetLastPInvokeError(), operation);

    private sealed class WindowsOwnedProcess : IDisposable
    {
        private SafeFileHandle? _job;
        private readonly SafeFileHandle _process;

        internal WindowsOwnedProcess(
            SafeFileHandle process,
            SafeFileHandle job,
            Stream standardInput,
            Stream standardOutput,
            Stream standardError)
        {
            _process = process;
            _job = job;
            StandardInput = standardInput;
            StandardOutput = standardOutput;
            StandardError = standardError;
        }

        internal Stream StandardInput { get; }

        internal Stream StandardOutput { get; }

        internal Stream StandardError { get; }

        internal Task<int> WaitForExitAndCloseJobAsync() => Task.Run(() =>
        {
            WaitForExit();
            if (!GetExitCodeProcess(_process, out uint exitCode))
                throw CreateWin32Exception("GetExitCodeProcess");
            CloseJob();
            return unchecked((int)exitCode);
        });

        internal Task WaitForExitAsync() => Task.Run(WaitForExit);

        internal void Stop()
        {
            CloseJob();
            StandardInput.Dispose();
            StandardOutput.Dispose();
            StandardError.Dispose();
        }

        public void Dispose()
        {
            Stop();
            _process.Dispose();
        }

        private void WaitForExit()
        {
            if (WaitForSingleObject(_process, Infinite) != WaitObject0)
                throw CreateWin32Exception("WaitForSingleObject");
        }

        private void CloseJob()
        {
            SafeFileHandle? job = Interlocked.Exchange(ref _job, null);
            job?.Dispose();
        }
    }

    private sealed class OutputLimitExceededException(string streamName) : Exception
    {
        internal string StreamName { get; } = streamName;
    }

    private sealed class SafeHGlobalBuffer : SafeBuffer
    {
        internal SafeHGlobalBuffer(byte[] bytes)
            : base(ownsHandle: true)
        {
            SetHandle(Marshal.AllocHGlobal(bytes.Length));
            Initialize((ulong)bytes.Length);
            Marshal.Copy(bytes, 0, handle, bytes.Length);
        }

        protected override bool ReleaseHandle()
        {
            Marshal.FreeHGlobal(handle);
            return true;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SecurityAttributes
    {
        internal int nLength;
        internal IntPtr lpSecurityDescriptor;
        [MarshalAs(UnmanagedType.Bool)]
        internal bool bInheritHandle;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct StartupInfo
    {
        internal uint cb;
        internal string? lpReserved;
        internal string? lpDesktop;
        internal string? lpTitle;
        internal uint dwX;
        internal uint dwY;
        internal uint dwXSize;
        internal uint dwYSize;
        internal uint dwXCountChars;
        internal uint dwYCountChars;
        internal uint dwFillAttribute;
        internal uint dwFlags;
        internal ushort wShowWindow;
        internal ushort cbReserved2;
        internal IntPtr lpReserved2;
        internal IntPtr hStdInput;
        internal IntPtr hStdOutput;
        internal IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessInformation
    {
        internal IntPtr hProcess;
        internal IntPtr hThread;
        internal uint dwProcessId;
        internal uint dwThreadId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JobObjectBasicLimitInformation
    {
        internal long PerProcessUserTimeLimit;
        internal long PerJobUserTimeLimit;
        internal uint LimitFlags;
        internal UIntPtr MinimumWorkingSetSize;
        internal UIntPtr MaximumWorkingSetSize;
        internal uint ActiveProcessLimit;
        internal UIntPtr Affinity;
        internal uint PriorityClass;
        internal uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IoCounters
    {
        internal ulong ReadOperationCount;
        internal ulong WriteOperationCount;
        internal ulong OtherOperationCount;
        internal ulong ReadTransferCount;
        internal ulong WriteTransferCount;
        internal ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JobObjectExtendedLimitInformation
    {
        internal JobObjectBasicLimitInformation BasicLimitInformation;
        internal IoCounters IoInfo;
        internal UIntPtr ProcessMemoryLimit;
        internal UIntPtr JobMemoryLimit;
        internal UIntPtr PeakProcessMemoryUsed;
        internal UIntPtr PeakJobMemoryUsed;
    }

    private enum JobObjectInfoClass
    {
        ExtendedLimitInformation = 9,
    }

    [DllImport("kernel32.dll", EntryPoint = "CreatePipe", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreatePipe(
        out IntPtr readPipe,
        out IntPtr writePipe,
        ref SecurityAttributes attributes,
        uint size);

    [DllImport("kernel32.dll", EntryPoint = "SetHandleInformation", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetHandleInformation(
        SafeFileHandle handle,
        uint mask,
        uint flags);

    [DllImport("kernel32.dll", EntryPoint = "CreateProcessW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateProcessW(
        string? applicationName,
        StringBuilder commandLine,
        IntPtr processAttributes,
        IntPtr threadAttributes,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandles,
        uint creationFlags,
        IntPtr environment,
        string? currentDirectory,
        ref StartupInfo startupInfo,
        out ProcessInformation processInformation);

    [DllImport("kernel32.dll", EntryPoint = "ResumeThread", SetLastError = true)]
    private static extern uint ResumeThread(SafeFileHandle thread);

    [DllImport("kernel32.dll", EntryPoint = "TerminateProcess", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TerminateProcess(SafeFileHandle process, uint exitCode);

    [DllImport("kernel32.dll", EntryPoint = "WaitForSingleObject", SetLastError = true)]
    private static extern uint WaitForSingleObject(SafeFileHandle handle, uint milliseconds);

    [DllImport("kernel32.dll", EntryPoint = "GetExitCodeProcess", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetExitCodeProcess(SafeFileHandle process, out uint exitCode);

    [DllImport("kernel32.dll", EntryPoint = "CreateJobObjectW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateJobObjectW(IntPtr attributes, string? name);

    [DllImport("kernel32.dll", EntryPoint = "SetInformationJobObject", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetInformationJobObject(
        SafeFileHandle job,
        JobObjectInfoClass informationClass,
        ref JobObjectExtendedLimitInformation information,
        uint informationLength);

    [DllImport("kernel32.dll", EntryPoint = "AssignProcessToJobObject", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AssignProcessToJobObject(
        SafeFileHandle job,
        SafeFileHandle process);
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
