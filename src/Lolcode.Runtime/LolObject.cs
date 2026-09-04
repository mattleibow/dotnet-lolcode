namespace Lolcode.Runtime;

/// <summary>Represents a runtime LOLCODE namespace.</summary>
[System.Diagnostics.DebuggerNonUserCode]
public class LolScope
{
    internal Dictionary<string, object?> Values { get; } = new(StringComparer.Ordinal);
    internal LolScope? Parent { get; }
    internal LolObject? Caller { get; }

    /// <summary>Gets or sets the implicit IT value for this scope.</summary>
    public object? It { get; set; }

    /// <summary>Creates a namespace with optional lexical and calling-object parents.</summary>
    [System.Diagnostics.DebuggerStepThrough]
    public LolScope(LolScope? parent = null, LolObject? caller = null)
    {
        Parent = parent;
        Caller = caller;
    }
}

/// <summary>Represents a LOLCODE BUKKIT and its prototype chain.</summary>
public sealed class LolObject : LolScope
{
    internal LolScope? Prototype { get; set; }

    /// <summary>Creates an empty BUKKIT with the supplied prototype and active calling BUKKIT.</summary>
    public LolObject(LolScope? prototype = null, LolObject? caller = null)
        : base(caller: caller) =>
        Prototype = prototype;
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
