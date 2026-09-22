namespace Lolcode.Runtime;

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
