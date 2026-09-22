
namespace Lolcode.Runtime;

public static partial class LolRuntime
{
    // ==================== Boolean Operations ====================

    /// <summary>BOTH OF a AN b (AND)</summary>
    public static bool And(object? a, object? b) => IsTruthy(a) && IsTruthy(b);

    /// <summary>EITHER OF a AN b (OR)</summary>
    public static bool Or(object? a, object? b) => IsTruthy(a) || IsTruthy(b);

    /// <summary>WON OF a AN b (XOR)</summary>
    public static bool Xor(object? a, object? b) => IsTruthy(a) ^ IsTruthy(b);

    /// <summary>NOT a</summary>
    public static bool Not(object? a) => !IsTruthy(a);
}
