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
