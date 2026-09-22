namespace Lolcode.Runtime;

/// <summary>Implemented by generated LOLCODE library export types.</summary>
public interface ILolcodeLibraryInstance : IDisposable
{
    /// <summary>Gets the persistent library BUKKIT for this instance.</summary>
    LolObject Library { get; }
}

/// <summary>Represents a runtime LOLCODE namespace.</summary>
[System.Diagnostics.DebuggerNonUserCode]
public class LolScope
{
    internal Dictionary<string, object?> Values { get; } = new(StringComparer.Ordinal);
    internal LolScope? Parent { get; }
    internal LolObject? Caller { get; }
    internal LolResourceTracker Resources { get; }
    internal LolcodeLibraryRegistry Libraries { get; }

    /// <summary>Gets or sets the implicit IT value for this scope.</summary>
    public object? It { get; set; }

    /// <summary>Creates a namespace with optional lexical and calling-object parents.</summary>
    [System.Diagnostics.DebuggerStepThrough]
    public LolScope(LolScope? parent = null, LolObject? caller = null)
    {
        Parent = parent;
        Caller = caller;
        Resources = parent?.Resources ?? caller?.Resources ?? new LolResourceTracker();
        Libraries = parent?.Libraries ?? caller?.Libraries ?? new LolcodeLibraryRegistry();
    }

    internal LolScope(
        LolScope? parent,
        LolObject? caller,
        LolResourceTracker resources,
        LolcodeLibraryRegistry libraries)
    {
        Parent = parent;
        Caller = caller;
        Resources = resources;
        Libraries = libraries;
    }
}

internal sealed record LolcodeLibraryDefinition(string AssemblyName, string TypeName);

internal sealed class LolcodeLibraryRegistry
{
    private readonly Dictionary<string, LolcodeLibraryDefinition> _definitions =
        new(StringComparer.Ordinal);

    internal void Register(string name, string assemblyName, string typeName)
    {
        var definition = new LolcodeLibraryDefinition(assemblyName, typeName);
        if (_definitions.TryGetValue(name, out LolcodeLibraryDefinition? existing))
        {
            if (existing == definition)
                return;
            throw new LolRuntimeException($"Ambiguous LOLCODE library: {name}");
        }
        _definitions.Add(name, definition);
    }

    internal bool TryGet(string name, out LolcodeLibraryDefinition definition) =>
        _definitions.TryGetValue(name, out definition!);
}

/// <summary>Represents a LOLCODE BUKKIT and its prototype chain.</summary>
public sealed class LolObject : LolScope
{
    internal LolScope? Prototype { get; set; }
    internal bool IsLibraryModule { get; set; }

    /// <summary>Creates an empty BUKKIT with the supplied prototype and active calling BUKKIT.</summary>
    public LolObject(LolScope? prototype = null, LolObject? caller = null)
        : base(parent: prototype, caller: caller) =>
        Prototype = prototype;
}

internal sealed class LolResourceTracker
{
    private readonly object _gate = new();
    private readonly HashSet<LolBlob> _resources = new(ReferenceEqualityComparer.Instance);
    private bool _disposed;
    private readonly List<IDisposable> _libraries = [];

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

/// <summary>Incrementally resolves an identifier path in evaluation order.</summary>
public sealed class LolIdentifierResolver
{
    internal LolScope EvaluationScope { get; }
    internal LolScope Current { get; set; }
    internal string? Name { get; set; }
    internal int SegmentCount { get; set; }
    internal bool Traversed { get; set; }

    internal LolIdentifierResolver(LolScope evaluationScope, LolScope destination)
    {
        EvaluationScope = evaluationScope;
        Current = destination;
    }
}

/// <summary>A captured terminal binding location.</summary>
public sealed class LolResolvedSlot
{
    internal LolScope Owner { get; }
    internal string Name { get; }
    internal bool IsIt { get; }
    internal bool Rebaseable { get; }

    internal LolResolvedSlot(
        LolScope owner,
        string name,
        bool isIt = false,
        bool rebaseable = false)
    {
        Owner = owner;
        Name = name;
        IsIt = isIt;
        Rebaseable = rebaseable;
    }
}

/// <summary>The executable body stored by a LOLCODE function value.</summary>
/// <param name="scope">The caller's scope.</param>
/// <param name="receiver">The calling BUKKIT, if any.</param>
/// <param name="arguments">The evaluated arguments.</param>
/// <returns>The function result.</returns>
public delegate object? LolFunctionBody(
    LolScope scope,
    LolObject? receiver,
    object?[] arguments,
    LolResolvedSlot[] parameterNames);

/// <summary>Resolves one invocation parameter name in the caller's scope.</summary>
/// <param name="scope">The caller's scope.</param>
/// <returns>The captured parameter binding location.</returns>
public delegate LolResolvedSlot LolParameterNameResolver(LolScope scope);

/// <summary>Represents a first-class LOLCODE function value.</summary>
public sealed class LolFunction
{
    internal LolFunctionBody Body { get; }
    internal LolParameterNameResolver[] ParameterNameResolvers { get; }

    /// <summary>Gets the number of required arguments.</summary>
    public int Arity { get; }

    /// <summary>Creates a function value.</summary>
    public LolFunction(
        int arity,
        LolFunctionBody body,
        LolParameterNameResolver[] parameterNameResolvers)
    {
        Arity = arity;
        Body = body;
        ParameterNameResolvers = parameterNameResolvers;
    }

}

/// <summary>A function and receiver captured before call arguments are evaluated.</summary>
public sealed class LolFunctionTarget
{
    internal LolFunction Function { get; }
    internal LolObject? Receiver { get; }

    internal LolFunctionTarget(LolFunction function, LolObject? receiver)
    {
        Function = function;
        Receiver = receiver;
    }
}
