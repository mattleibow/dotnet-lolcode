using Lolcode.Runtime;
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

/// <summary>Creates the official STDIO provider for static LOLCODE imports.</summary>
public static class StdioLibraryFactory
{
    /// <summary>Creates a scope-bound STDIO module without runtime discovery.</summary>
    /// <param name="scope">The importing LOLCODE scope.</param>
    /// <returns>The STDIO module.</returns>
    public static LolObject Create(LolScope scope)
    {
        var builder = new LolcodeLibraryBuilder(scope);
        builder.AddFunction("OPEN", 2, static (context, arguments) =>
            StdioLibrary.OPEN(
                context,
                LolRuntime.CastToYarn(arguments[0]),
                LolRuntime.CastToYarn(arguments[1])));
        builder.AddFunction("DIAF", 1, static (_, arguments) => StdioLibrary.DIAF(arguments[0]!));
        builder.AddFunction("LUK", 2, static (_, arguments) =>
            StdioLibrary.LUK(arguments[0]!, LolRuntime.CastToNumbr(arguments[1])));
        builder.AddFunction("SCRIBBEL", 2, static (_, arguments) =>
        {
            StdioLibrary.SCRIBBEL(arguments[0]!, arguments[1]!);
            return null;
        });
        builder.AddFunction("AGEIN", 1, static (_, arguments) =>
        {
            StdioLibrary.AGEIN(arguments[0]!);
            return null;
        });
        builder.AddFunction("CLOSE", 1, static (_, arguments) =>
        {
            StdioLibrary.CLOSE(arguments[0]!);
            return null;
        });
        return builder.Build();
    }
}
