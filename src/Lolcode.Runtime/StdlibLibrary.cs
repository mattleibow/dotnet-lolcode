namespace Lolcode.Runtime;

internal sealed class RandomState
{
    private readonly object _gate = new();
    private Random _random = new();

    internal void Seed(int seed)
    {
        lock (_gate)
            _random = new Random(seed);
    }

    internal int Next(int maximum)
    {
        if (maximum <= 0)
            return 0;
        lock (_gate)
            return _random.Next(maximum);
    }
}

[LolcodeLibrary("STDLIB")]
public sealed class StdlibLibrary
{
    private readonly RandomState _random = new();

    /// <summary>Seeds this library instance's random generator.</summary>
    public void MIX(int seed) => _random.Seed(seed);

    /// <summary>Gets a random value from this library instance.</summary>
    public int BLOW(int maximum) => _random.Next(maximum);
}
