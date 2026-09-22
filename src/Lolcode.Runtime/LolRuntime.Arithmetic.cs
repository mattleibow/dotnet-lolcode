
using System.Globalization;
using System.Text;

namespace Lolcode.Runtime;

public static partial class LolRuntime
{
    // ==================== Arithmetic ====================

    /// <summary>
    /// Coerces a value to a numeric type for arithmetic.
    /// NOOB → runtime error (per spec: "Any operations on a NOOB that assume another type result in an error").
    /// Non-numeric YARN → runtime error.
    /// bool: WIN→1, FAIL→0.
    /// </summary>
    private static object CoerceToNumeric(object? value)
    {
        return value switch
        {
            int => value,
            double => value,
            bool b => b ? 1 : 0,
            YarnLiteral yarn => CoerceToNumeric(ResolveUnicodeEscapes(yarn.Value)),
            LolByteYarn yarn => CoerceToNumeric(Encoding.Latin1.GetString(yarn.Bytes)),
            string s when s.Contains('.') => double.TryParse(s, CultureInfo.InvariantCulture, out double d)
                ? (object)d
                : throw new LolRuntimeException("Cannot cast YARN to numeric: " + s),
            string s => int.TryParse(s, CultureInfo.InvariantCulture, out int i)
                ? (object)i
                : throw new LolRuntimeException("Cannot cast YARN to numeric: " + s),
            null => throw new LolRuntimeException("Cannot use NOOB in arithmetic"),
            _ => throw new LolRuntimeException("Cannot use value in arithmetic: " + value)
        };
    }

    /// <summary>
    /// Determines if the result should be NUMBAR (double).
    /// If either operand is double, result is double.
    /// </summary>
    private static bool IsFloatOperation(object a, object b) => a is double || b is double;

    /// <summary>SUM OF a AN b</summary>
    public static object Add(object? a, object? b)
    {
        var ca = CoerceToNumeric(a);
        var cb = CoerceToNumeric(b);
        if (IsFloatOperation(ca, cb))
            return CastToNumbar(ca) + CastToNumbar(cb);
        return CastToNumbr(ca) + CastToNumbr(cb);
    }

    /// <summary>DIFF OF a AN b</summary>
    public static object Subtract(object? a, object? b)
    {
        var ca = CoerceToNumeric(a);
        var cb = CoerceToNumeric(b);
        if (IsFloatOperation(ca, cb))
            return CastToNumbar(ca) - CastToNumbar(cb);
        return CastToNumbr(ca) - CastToNumbr(cb);
    }

    /// <summary>PRODUKT OF a AN b</summary>
    public static object Multiply(object? a, object? b)
    {
        var ca = CoerceToNumeric(a);
        var cb = CoerceToNumeric(b);
        if (IsFloatOperation(ca, cb))
            return CastToNumbar(ca) * CastToNumbar(cb);
        return CastToNumbr(ca) * CastToNumbr(cb);
    }

    /// <summary>QUOSHUNT OF a AN b</summary>
    public static object Divide(object? a, object? b)
    {
        var ca = CoerceToNumeric(a);
        var cb = CoerceToNumeric(b);
        if (IsFloatOperation(ca, cb))
        {
            double divisor = CastToNumbar(cb);
            if (divisor == 0.0)
                throw new LolRuntimeException("Division by zero");
            return CastToNumbar(ca) / divisor;
        }
        int intDivisor = CastToNumbr(cb);
        if (intDivisor == 0)
            throw new LolRuntimeException("Division by zero");
        return CastToNumbr(ca) / intDivisor;
    }

    /// <summary>MOD OF a AN b</summary>
    public static object Modulo(object? a, object? b)
    {
        var ca = CoerceToNumeric(a);
        var cb = CoerceToNumeric(b);
        if (IsFloatOperation(ca, cb))
        {
            double divisor = CastToNumbar(cb);
            if (divisor == 0.0)
                throw new LolRuntimeException("Modulo by zero");
            return CastToNumbar(ca) % divisor;
        }
        int intDivisor = CastToNumbr(cb);
        if (intDivisor == 0)
            throw new LolRuntimeException("Modulo by zero");
        return CastToNumbr(ca) % intDivisor;
    }

    /// <summary>BIGGR OF a AN b</summary>
    public static object Greater(object? a, object? b)
    {
        var ca = CoerceToNumeric(a);
        var cb = CoerceToNumeric(b);
        if (IsFloatOperation(ca, cb))
        {
            double da = CastToNumbar(ca), db = CastToNumbar(cb);
            return da >= db ? da : db;
        }
        int ia = CastToNumbr(ca), ib = CastToNumbr(cb);
        return ia >= ib ? ia : ib;
    }

    /// <summary>SMALLR OF a AN b</summary>
    public static object Smaller(object? a, object? b)
    {
        var ca = CoerceToNumeric(a);
        var cb = CoerceToNumeric(b);
        if (IsFloatOperation(ca, cb))
        {
            double da = CastToNumbar(ca), db = CastToNumbar(cb);
            return da <= db ? da : db;
        }
        int ia = CastToNumbr(ca), ib = CastToNumbr(cb);
        return ia <= ib ? ia : ib;
    }
}
