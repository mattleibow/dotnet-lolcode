namespace Lolcode.Runtime;

internal sealed class LolResourceTracker
{
    private readonly object _gate = new();
    private readonly HashSet<LolBlob> _resources = new(ReferenceEqualityComparer.Instance);
    private readonly List<IDisposable> _libraries = [];
    private bool _disposed;

    internal void RegisterLibrary(IDisposable library)
    {
        lock (_gate)
        {
            if (_disposed)
            {
                library.Dispose();
                throw new ObjectDisposedException(nameof(LolScope));
            }
            _libraries.Add(library);
        }
    }

    internal T Register<T>(T resource) where T : LolBlob
    {
        lock (_gate)
        {
            if (resource.IsClosed)
                return resource;
            if (resource.IsTrackedBy(this))
                return resource;
            if (resource.IsTracked)
                throw new InvalidOperationException("BLOB handle is owned by a different LOLCODE scope.");
            if (_disposed)
            {
                resource.Dispose();
                throw new ObjectDisposedException(nameof(LolScope));
            }
            if (!resource.TryAttachTracker(this))
                return resource;
            _resources.Add(resource);
        }
        return resource;
    }

    internal void Unregister(LolBlob resource)
    {
        lock (_gate)
            _resources.Remove(resource);
    }

    internal void Detach(LolBlob resource)
    {
        lock (_gate)
            _resources.Remove(resource);
        resource.DetachTracker(this);
    }

    internal void Dispose()
    {
        LolBlob[] resources;
        lock (_gate)
        {
            if (_disposed)
                return;
            _disposed = true;
            resources = [.. _resources];
            _resources.Clear();
        }

        foreach (LolBlob resource in resources)
            resource.Dispose();
        foreach (IDisposable library in _libraries)
            library.Dispose();
    }

    internal void ThrowIfDisposed()
    {
        lock (_gate)
        {
            if (_disposed)
                throw new ObjectDisposedException("LOLCODE library instance");
        }
    }
}
