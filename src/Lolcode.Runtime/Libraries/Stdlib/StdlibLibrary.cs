namespace Lolcode.Runtime;

[LolcodeLibrary("STDLIB")]
public sealed class StdlibLibrary
{
    private readonly RandomState _random = new();

    /// <summary>Seeds this library instance's random generator.</summary>
    public void MIX(int seed) => _random.Seed(seed);

    /// <summary>Gets a random value from this library instance.</summary>
    public int BLOW(int maximum) => _random.Next(maximum);
}
