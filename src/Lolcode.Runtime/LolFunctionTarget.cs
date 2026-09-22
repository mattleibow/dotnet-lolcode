namespace Lolcode.Runtime;

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
