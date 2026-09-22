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

/// <summary>Creates the official STDLIB provider for static LOLCODE imports.</summary>
public static class StdlibLibraryFactory
{
    /// <summary>Creates a scope-bound STDLIB module without runtime discovery.</summary>
    /// <param name="scope">The importing LOLCODE scope.</param>
    /// <returns>The STDLIB module.</returns>
    public static LolObject Create(LolScope scope)
    {
        var builder = new LolcodeLibraryBuilder(scope);
        builder.AddFunction("MIX", 1, static (context, arguments) =>
        {
            StdlibLibrary.MIX(context, LolRuntime.CastToNumbr(arguments[0]));
            return null;
        });
        builder.AddFunction("BLOW", 1, static (context, arguments) =>
            StdlibLibrary.BLOW(context, LolRuntime.CastToNumbr(arguments[0])));
        return builder.Build();
    }
}
