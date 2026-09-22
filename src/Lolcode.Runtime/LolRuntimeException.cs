namespace Lolcode.Runtime;

/// <summary>
/// Exception thrown for runtime errors in LOLCODE programs.
/// </summary>
public class LolRuntimeException : Exception
{
    public LolRuntimeException(string message) : base(message) { }

    public LolRuntimeException(string message, Exception inner) : base(message, inner) { }
}
