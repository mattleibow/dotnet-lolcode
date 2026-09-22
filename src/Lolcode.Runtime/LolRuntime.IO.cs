using System.Text;

namespace Lolcode.Runtime;

public static partial class LolRuntime
{
    private static readonly AsyncLocal<IoContext?> CurrentIo = new();

    // ==================== I/O ====================

    /// <summary>
    /// Overrides the input, standard output, and standard error used by LOLCODE I/O within the current asynchronous context.
    /// </summary>
    /// <param name="input">The reader used by <c>GIMMEH</c>.</param>
    /// <param name="standardOutput">The writer used by <c>VISIBLE</c>.</param>
    /// <param name="standardError">The writer used by <c>INVISIBLE</c> and system-command standard error.</param>
    /// <returns>A scope that restores the previous I/O when disposed.</returns>
    /// <remarks>
    /// Scopes may be nested and must be disposed in reverse order. When no scope is active,
    /// LOLCODE programs use <see cref="Console.In"/>, <see cref="Console.Out"/>, and <see cref="Console.Error"/>.
    /// </remarks>
    public static IDisposable PushIo(TextReader input, TextWriter standardOutput, TextWriter standardError)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(standardOutput);
        ArgumentNullException.ThrowIfNull(standardError);

        var previous = CurrentIo.Value;
        var current = new IoContext(input, standardOutput, standardError);
        CurrentIo.Value = current;
        return new IoScope(previous, current);
    }

    /// <summary>
    /// VISIBLE: print arguments concatenated as YARN.
    /// </summary>
    public static void Print(object?[] args, bool suppressNewline) =>
        Print(args, suppressNewline, standardError: false);

    /// <summary>
    /// VISIBLE or INVISIBLE: print arguments concatenated as YARN to the selected stream.
    /// </summary>
    public static void Print(object?[] args, bool suppressNewline, bool standardError)
    {
        TextWriter writer = standardError
            ? CurrentIo.Value?.StandardError ?? Console.Error
            : CurrentIo.Value?.StandardOutput ?? Console.Out;
        if (args.Any(static arg => arg is LolByteYarn))
        {
            byte[] bytes = GetYarnBytes(ConcatenateYarns(args));
            YarnByteSink.Write(writer, bytes, suppressNewline);
            return;
        }

        var sb = new System.Text.StringBuilder();
        foreach (var arg in args)
            sb.Append(CastToYarn(arg));

        if (suppressNewline)
            writer.Write(sb.ToString());
        else
            writer.WriteLine(sb.ToString());
    }

    /// <summary>Executes a command through the host shell and returns standard output as text.</summary>
    public static string ExecuteSystemCommand(object? command) =>
        DecodeProcessOutput(GetYarnBytes(ExecuteSystemCommandValue(command)));

    /// <summary>
    /// Executes a command through the host shell and returns standard output while preserving its bytes.
    /// </summary>
    public static object ExecuteSystemCommandValue(object? command)
    {
        string commandText = CastToYarn(ExplicitCast(command, "YARN"));
        bool windows = OperatingSystem.IsWindows();
        string shell = windows
            ? Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe"
            : "/bin/sh";
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = shell,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add(windows ? "/C" : "-c");
        startInfo.ArgumentList.Add(commandText);

        try
        {
            using var process = System.Diagnostics.Process.Start(startInfo)
                ?? throw new LolRuntimeException($"Unable to launch system shell: {shell}");
            Task<byte[]> output = ReadAllBytesAsync(process.StandardOutput.BaseStream);
            Task<byte[]> error = ReadAllBytesAsync(process.StandardError.BaseStream);
            process.WaitForExit();
            byte[] outputBytes = output.GetAwaiter().GetResult();
            byte[] errorBytes = error.GetAwaiter().GetResult();
            if (errorBytes.Length > 0)
                YarnByteSink.Write(
                    CurrentIo.Value?.StandardError ?? Console.Error,
                    errorBytes,
                    suppressNewline: true);
            return new LolByteYarn(outputBytes);
        }
        catch (LolRuntimeException)
        {
            throw;
        }
        catch (Exception ex) when (
            ex is InvalidOperationException or System.ComponentModel.Win32Exception or
                IOException or UnauthorizedAccessException)
        {
            throw new LolRuntimeException($"Unable to execute system command: {commandText}", ex);
        }
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream)
    {
        using var result = new MemoryStream();
        await stream.CopyToAsync(result);
        return result.ToArray();
    }

    private static string DecodeProcessOutput(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// GIMMEH: read a line of input.
    /// </summary>
    public static string ReadLine()
    {
        var input = CurrentIo.Value?.Input ?? Console.In;
        return input.ReadLine() ?? "";
    }

    private sealed record IoContext(
        TextReader Input,
        TextWriter StandardOutput,
        TextWriter StandardError);

    private sealed class IoScope(IoContext? previous, IoContext current) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;
            if (!ReferenceEquals(CurrentIo.Value, current))
                throw new InvalidOperationException("LOLCODE I/O scopes must be disposed in reverse order.");

            CurrentIo.Value = previous;
            _disposed = true;
        }
    }

    /// <summary>Writes the UTF-8 byte-order mark preserved from source.</summary>
    public static void WriteByteOrderMark() =>
        (CurrentIo.Value?.StandardOutput ?? Console.Out).Write('\uFEFF');
}
