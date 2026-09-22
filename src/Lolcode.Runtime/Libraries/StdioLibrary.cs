namespace Lolcode.Runtime;

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

[LolcodeLibrary("STDIO")]
public sealed class StdioLibrary
{
    /// <summary>Opens a file and returns its BLOB handle.</summary>
    public object OPEN(string filename, string mode)
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
            FileBlob blob = new(stream, failed: false, append);
            stream = null;
            return blob;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            stream?.Dispose();
            return new FileBlob(null, failed: true);
        }
    }

    /// <summary>Checks a file BLOB for errors.</summary>
    public bool DIAF(object file)
    {
        FileBlob blob = RequireFile(file);
        return blob.IsClosed || blob.Stream is null || blob.HasError;
    }

    /// <summary>Reads from a file BLOB.</summary>
    public object LUK(object file, int length)
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

    /// <summary>Writes to a file BLOB.</summary>
    public void SCRIBBEL(object file, object data)
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

    /// <summary>Rewinds a file BLOB.</summary>
    public void AGEIN(object file)
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

    /// <summary>Closes a file BLOB.</summary>
    public void CLOSE(object file) => RequireFile(file).Dispose();

    private static FileBlob RequireFile(object? value) =>
        value as FileBlob ?? throw new LolRuntimeException("Expected a file BLOB handle");

    private static object CreateYarn(byte[] data) => LolRuntime.CreateByteYarn(data);
}
