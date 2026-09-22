
namespace Lolcode.Runtime;

public static partial class LolRuntime
{
    // ==================== Comparison ====================

    /// <summary>
    /// BOTH SAEM: equality with NO auto-casting between different type families.
    /// NUMBR/NUMBAR promotes to NUMBAR. All other cross-type comparisons → FAIL.
    /// </summary>
    public static bool BothSaem(object? a, object? b)
    {
        a = ResolveYarnLiteral(a);
        b = ResolveYarnLiteral(b);

        if (a is null && b is null) return true;
        if (a is null || b is null) return false;

        if (IsYarn(a) && IsYarn(b))
            return GetYarnBytes(a).AsSpan().SequenceEqual(GetYarnBytes(b));

        // Same type comparison
        if (a.GetType() == b.GetType())
        {
            return a.Equals(b);
        }

        // NUMBR/NUMBAR cross-promotion
        if ((a is int || a is double) && (b is int || b is double))
        {
            return CastToNumbar(a) == CastToNumbar(b);
        }

        // Different type families → FAIL (no auto-casting)
        return false;
    }

    /// <summary>DIFFRINT: inequality (opposite of BOTH SAEM).</summary>
    public static bool Diffrint(object? a, object? b) => !BothSaem(a, b);

    /// <summary>Matches a WTF? case using exact runtime type and value equality.</summary>
    public static bool SwitchCaseMatches(object? value, object? caseValue)
    {
        value = ResolveYarnLiteral(value);
        caseValue = ResolveYarnLiteral(caseValue);

        if (value is null || caseValue is null)
            return value is null && caseValue is null;
        if (IsYarn(value) && IsYarn(caseValue))
            return GetYarnBytes(value).AsSpan().SequenceEqual(GetYarnBytes(caseValue));
        return value.GetType() == caseValue.GetType() && value.Equals(caseValue);
    }

    private static object? ResolveYarnLiteral(object? value) =>
        value switch
        {
            YarnLiteral yarn => ResolveUnicodeEscapes(yarn.Value),
            _ => value,
        };

    private static bool IsYarn(object value) => value is string or LolByteYarn;
}
