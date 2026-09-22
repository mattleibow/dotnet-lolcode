namespace Lolcode.Runtime;

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
