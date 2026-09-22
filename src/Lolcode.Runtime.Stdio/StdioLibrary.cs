using Lolcode.Runtime;

[assembly: LolcodeLibraryProvider(
    "STDIO",
    typeof(Lolcode.Runtime.Stdio.StdioLibrary),
    isBuiltIn: true)]

namespace Lolcode.Runtime.Stdio;

internal sealed class FileBlob(Stream? stream, bool failed, bool appendWrites = false) : LolBlob
{
    internal Stream? Stream { get; } = stream;
    internal bool HasError { get; set; } = failed;
    internal bool AppendWrites { get; } = appendWrites;

    internal Stream GetStream(string operation)
    {
        ThrowIfClosed(operation);
        return Stream ?? throw new LolRuntimeException($"Cannot {operation} a failed file BLOB handle");
    }

    protected override void DisposeCore()
    {
        try { Stream?.Dispose(); }
        catch { HasError = true; }
    }
}

internal static class StdioLibrary
{
    public static object OPEN(LolcodeLibraryContext context, string filename, string mode)
    {
        FileStream? stream = null;
        try
        {
            (FileMode fileMode, FileAccess access, bool append) = mode switch
            {
                "r" => (FileMode.Open, FileAccess.Read, false),
                "w" => (FileMode.Create, FileAccess.Write, false),
                "a" => (FileMode.OpenOrCreate, FileAccess.Write, true),
                "r+" => (FileMode.Open, FileAccess.ReadWrite, false),
                "w+" => (FileMode.Create, FileAccess.ReadWrite, false),
                "a+" => (FileMode.OpenOrCreate, FileAccess.ReadWrite, true),
                _ => throw new ArgumentException("Unsupported file mode", nameof(mode)),
            };
            stream = new FileStream(filename, fileMode, access, FileShare.ReadWrite);
            if (append)
                stream.Seek(0, SeekOrigin.End);
            FileBlob blob = context.RegisterResource(new FileBlob(stream, failed: false, append));
            stream = null;
            return blob;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            stream?.Dispose();
            return context.RegisterResource(new FileBlob(null, failed: true));
        }
    }

    public static bool DIAF(object file)
    {
        FileBlob blob = RequireFile(file);
        return blob.IsClosed || blob.Stream is null || blob.HasError;
    }

    public static object LUK(object file, int length)
    {
        FileBlob blob = RequireFile(file);
        if (length <= 0)
            return string.Empty;
        try
        {
            byte[] data = new byte[length];
            int read = blob.GetStream("read").Read(data);
            return read == 0 ? string.Empty : CreateYarn(data[..read]);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ObjectDisposedException)
        {
            blob.HasError = true;
            return string.Empty;
        }
    }

    public static void SCRIBBEL(object file, object data)
    {
        FileBlob blob = RequireFile(file);
        byte[] bytes = LolRuntime.GetExplicitYarnBytes(data);
        try
        {
            Stream stream = blob.GetStream("write");
            if (blob.AppendWrites)
                stream.Seek(0, SeekOrigin.End);
            stream.Write(bytes);
            stream.Flush();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ObjectDisposedException)
        {
            blob.HasError = true;
        }
    }

    public static void AGEIN(object file)
    {
        FileBlob blob = RequireFile(file);
        try
        {
            blob.GetStream("rewind").Seek(0, SeekOrigin.Begin);
            blob.HasError = false;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ObjectDisposedException)
        {
            blob.HasError = true;
        }
    }

    public static void CLOSE(object file) => RequireFile(file).Dispose();

    private static FileBlob RequireFile(object? value) =>
        value as FileBlob ?? throw new LolRuntimeException("Expected a file BLOB handle");

    private static object CreateYarn(byte[] data) => LolRuntime.CreateByteYarn(data);
}
