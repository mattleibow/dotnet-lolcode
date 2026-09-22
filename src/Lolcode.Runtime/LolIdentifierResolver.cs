namespace Lolcode.Runtime;

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
