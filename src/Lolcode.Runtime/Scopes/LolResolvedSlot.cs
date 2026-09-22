namespace Lolcode.Runtime;

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
