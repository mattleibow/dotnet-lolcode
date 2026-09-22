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
