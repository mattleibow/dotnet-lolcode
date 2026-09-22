using Lolcode.Runtime;

namespace Lolcode.Runtime.Stdlib;

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

internal static class StdlibLibrary
{
    public static void MIX(LolcodeLibraryContext context, int seed) =>
        context.GetOrCreateState(static () => new RandomState()).Seed(seed);

    public static int BLOW(LolcodeLibraryContext context, int maximum) =>
        context.GetOrCreateState(static () => new RandomState()).Next(maximum);
}
