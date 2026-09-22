namespace Lolcode.Runtime;

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
