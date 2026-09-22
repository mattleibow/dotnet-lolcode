namespace Lolcode.Runtime;

/// <summary>
/// Represents a managed opaque handle returned by a built-in LOLCODE library.
/// </summary>
public abstract class LolBlob : IDisposable
{
    private int _disposed;
    private LolResourceTracker? _tracker;

    /// <summary>Gets whether the handle has been closed.</summary>
    public bool IsClosed => Volatile.Read(ref _disposed) != 0;

    /// <summary>Closes the handle. Repeated calls are safe.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            LolResourceTracker? tracker = Interlocked.Exchange(ref _tracker, null);
            tracker?.Unregister(this);
            DisposeCore();
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases the managed operating-system resource.</summary>
    protected abstract void DisposeCore();

    /// <summary>Throws when a provider attempts an operation on a closed handle.</summary>
    protected void ThrowIfClosed(string operation)
    {
        if (IsClosed)
            throw new LolRuntimeException($"Cannot {operation} a closed BLOB handle");
    }

    internal bool TryAttachTracker(LolResourceTracker tracker)
    {
        if (IsClosed)
            return false;
        if (Interlocked.CompareExchange(ref _tracker, tracker, null) is not null)
            throw new InvalidOperationException("BLOB handle is already tracked.");
        if (!IsClosed)
            return true;

        Interlocked.CompareExchange(ref _tracker, null, tracker);
        return false;
    }

    internal bool IsTracked => Volatile.Read(ref _tracker) is not null;

    internal bool IsTrackedBy(LolResourceTracker tracker) =>
        ReferenceEquals(Volatile.Read(ref _tracker), tracker);

    internal void DetachTracker(LolResourceTracker tracker) =>
        Interlocked.CompareExchange(ref _tracker, null, tracker);
}
