using System.Globalization;

namespace Lolcode.Runtime;

/// <summary>
/// Runtime support library for compiled LOLCODE programs.
/// All LOLCODE values are represented as <see cref="object"/> at runtime.
/// Types: <see cref="int"/> (NUMBR), <see cref="double"/> (NUMBAR),
/// <see cref="string"/> (YARN), <see cref="bool"/> (TROOF), or <c>null</c> (NOOB).
/// </summary>
public static class LolRuntime
{
    private sealed record YarnLiteral(string Value);

    // ==================== Type Coercion ====================

    /// <summary>
    /// Casts a value to TROOF (boolean).
    /// NOOB → FAIL, 0 → FAIL, 0.0 → FAIL, "" → FAIL, everything else → WIN.
    /// </summary>
    public static bool IsTruthy(object? value)
    {
        return value switch
        {
            null => false,
            bool b => b,
            int i => i != 0,
            double d => d != 0.0,
            string s => s.Length > 0,
            YarnLiteral yarn => yarn.Value.Length > 0,
            _ => true
        };
    }

    /// <summary>
    /// Casts a value to NUMBR (int).
    /// </summary>
    public static int CastToNumbr(object? value)
    {
        return value switch
        {
            null => 0,
            int i => i,
            double d => (int)d,
            bool b => b ? 1 : 0,
            string s => ParseNumbrPrefix(s),
            YarnLiteral yarn => ParseNumbrPrefix(ResolveUnicodeEscapes(yarn.Value)),
            _ => 0
        };
    }

    /// <summary>
    /// Casts a value to NUMBAR (double).
    /// </summary>
    public static double CastToNumbar(object? value)
    {
        return value switch
        {
            null => 0.0,
            int i => (double)i,
            double d => d,
            bool b => b ? 1.0 : 0.0,
            string s => ParseNumbarPrefix(s),
            YarnLiteral yarn => ParseNumbarPrefix(ResolveUnicodeEscapes(yarn.Value)),
            _ => 0.0
        };
    }

    /// <summary>
    /// Casts a value to YARN (string).
    /// NUMBAR is truncated to 2 decimal places.
    /// </summary>
    public static string CastToYarn(object? value)
    {
        return value switch
        {
            null => throw new LolRuntimeException("Cannot cast NOOB to YARN"),
            bool => throw new LolRuntimeException("Cannot cast TROOF to YARN"),
            int i => i.ToString(CultureInfo.InvariantCulture),
            double d => FormatNumbar(d),
            string s => s,
            YarnLiteral yarn => ResolveYarnLiteral(yarn.Value),
            _ => value.ToString() ?? ""
        };
    }

    /// <summary>Creates a source YARN whose Unicode escapes resolve when the value is used.</summary>
    public static object CreateYarnLiteral(string value) => new YarnLiteral(value);

    /// <summary>Resolves Unicode escapes in a source YARN literal.</summary>
    public static string ResolveYarnLiteral(string value) => ResolveUnicodeEscapes(value);

    private static string ResolveUnicodeEscapes(string value)
    {
        if (!value.Contains(":(", StringComparison.Ordinal) &&
            !value.Contains(":[", StringComparison.Ordinal))
        {
            return value;
        }

        var result = new System.Text.StringBuilder(value.Length);
        for (int index = 0; index < value.Length;)
        {
            if (index + 2 < value.Length && value[index] == ':' && value[index + 1] == '(')
            {
                int end = value.IndexOf(')', index + 2);
                if (end < 0)
                    throw new LolRuntimeException("Invalid Unicode code point.");

                ReadOnlySpan<char> hex = value.AsSpan(index + 2, end - index - 2);
                if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int codePoint) ||
                    !System.Text.Rune.IsValid(codePoint))
                {
                    throw new LolRuntimeException("Invalid Unicode code point.");
                }

                result.Append(char.ConvertFromUtf32(codePoint));
                index = end + 1;
                continue;
            }

            if (index + 2 < value.Length && value[index] == ':' && value[index + 1] == '[')
            {
                int end = value.IndexOf(']', index + 2);
                if (end < 0)
                    throw new LolRuntimeException("Invalid Unicode normative name.");

                string name = value[(index + 2)..end];
                char? character = ResolveUnicodeName(name);
                if (!character.HasValue)
                    throw new LolRuntimeException($"Invalid Unicode normative name: {name}.");

                result.Append(character.Value);
                index = end + 1;
                continue;
            }

            result.Append(value[index]);
            index++;
        }

        return result.ToString();
    }

    private static char? ResolveUnicodeName(string name)
    {
        return name switch
        {
            "SPACE" => ' ',
            "TAB" or "CHARACTER TABULATION" => '\t',
            "NEWLINE" or "LINE FEED" or "LINE FEED (LF)" => '\n',
            "CARRIAGE RETURN" or "CARRIAGE RETURN (CR)" => '\r',
            "NULL" => '\0',
            "BELL" => '\a',
            "BACKSPACE" => '\b',
            "FORM FEED" or "FORM FEED (FF)" => '\f',
            "VERTICAL TAB" or "LINE TABULATION" => '\v',
            "QUOTATION MARK" => '"',
            "COLON" => ':',
            "EXCLAMATION MARK" => '!',
            "QUESTION MARK" => '?',
            "NUMBER SIGN" => '#',
            "DOLLAR SIGN" => '$',
            "CENT SIGN" => '\u00A2',
            "EURO SIGN" => '\u20AC',
            "PERCENT SIGN" => '%',
            "AMPERSAND" => '&',
            "APOSTROPHE" => '\'',
            "LEFT PARENTHESIS" => '(',
            "RIGHT PARENTHESIS" => ')',
            "ASTERISK" => '*',
            "PLUS SIGN" => '+',
            "COMMA" => ',',
            "HYPHEN-MINUS" => '-',
            "FULL STOP" => '.',
            "SOLIDUS" => '/',
            _ => null,
        };
    }

    private static int ParseNumbrPrefix(string value)
    {
        ReadOnlySpan<char> text = value.AsSpan().TrimStart();
        if (text.IsEmpty)
            return 0;

        int sign = 1;
        if (text[0] is '+' or '-')
        {
            sign = text[0] == '-' ? -1 : 1;
            text = text[1..];
        }

        int numberBase = 10;
        int prefixLength = 0;
        if (text.Length >= 2 && text[0] == '0' && text[1] is 'x' or 'X')
        {
            numberBase = 16;
            prefixLength = 2;
        }
        else if (text.Length > 1 && text[0] == '0')
        {
            numberBase = 8;
        }

        int digitCount = 0;
        long result = 0;
        for (int index = prefixLength; index < text.Length; index++)
        {
            int digit = DigitValue(text[index]);
            if (digit < 0 || digit >= numberBase)
                break;

            digitCount++;
            result = result * numberBase + digit;
            if (result > (long)int.MaxValue + (sign < 0 ? 1L : 0L))
                return sign < 0 ? int.MinValue : int.MaxValue;
        }

        if (digitCount == 0)
            return 0;

        long signed = sign * result;
        return (int)Math.Clamp(signed, int.MinValue, int.MaxValue);
    }

    private static int DigitValue(char character) => character switch
    {
        >= '0' and <= '9' => character - '0',
        >= 'a' and <= 'f' => character - 'a' + 10,
        >= 'A' and <= 'F' => character - 'A' + 10,
        _ => -1,
    };

    private static double ParseNumbarPrefix(string value)
    {
        ReadOnlySpan<char> text = value.AsSpan().TrimStart();
        int index = 0;
        if (index < text.Length && text[index] is '+' or '-')
            index++;

        int wholeDigits = ConsumeDecimalDigits(text, ref index);
        int fractionalDigits = 0;
        if (index < text.Length && text[index] == '.')
        {
            index++;
            fractionalDigits = ConsumeDecimalDigits(text, ref index);
        }

        if (wholeDigits == 0 && fractionalDigits == 0)
            return 0.0;

        int exponentStart = index;
        if (index < text.Length && text[index] is 'e' or 'E')
        {
            index++;
            if (index < text.Length && text[index] is '+' or '-')
                index++;
            if (ConsumeDecimalDigits(text, ref index) == 0)
                index = exponentStart;
        }

        return double.TryParse(
            text[..index],
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out double result)
            ? result
            : 0.0;
    }

    private static int ConsumeDecimalDigits(ReadOnlySpan<char> text, ref int index)
    {
        int start = index;
        while (index < text.Length && text[index] is >= '0' and <= '9')
            index++;
        return index - start;
    }

    private static string FormatNumbar(double value)
    {
        if (double.IsFinite(value) && Math.Abs(value) <= (double)(decimal.MaxValue / 100m))
        {
            var truncated = decimal.Truncate((decimal)value * 100m) / 100m;
            return truncated.ToString("F2", CultureInfo.InvariantCulture);
        }

        return value.ToString("F2", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Casts a value to TROOF.
    /// </summary>
    public static bool CastToTroof(object? value) => IsTruthy(value);

    /// <summary>
    /// Performs an explicit MAEK or IS NOW A cast.
    /// </summary>
    public static object? ExplicitCast(object? value, string targetType)
    {
        return targetType switch
        {
            "TROOF" => (object)CastToTroof(value),
            "NUMBR" => (object)CastToNumbr(value),
            "NUMBAR" => (object)CastToNumbar(value),
            "YARN" when value is null => string.Empty,
            "YARN" => (object)CastToYarn(value),
            "NOOB" => null,
            _ => throw new InvalidOperationException($"Unknown type: {targetType}")
        };
    }

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

    // ==================== Boolean Operations ====================

    /// <summary>BOTH OF a AN b (AND)</summary>
    public static bool And(object? a, object? b) => IsTruthy(a) && IsTruthy(b);

    /// <summary>EITHER OF a AN b (OR)</summary>
    public static bool Or(object? a, object? b) => IsTruthy(a) || IsTruthy(b);

    /// <summary>WON OF a AN b (XOR)</summary>
    public static bool Xor(object? a, object? b) => IsTruthy(a) ^ IsTruthy(b);

    /// <summary>NOT a</summary>
    public static bool Not(object? a) => !IsTruthy(a);

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
        return value.GetType() == caseValue.GetType() && value.Equals(caseValue);
    }

    private static object? ResolveYarnLiteral(object? value) =>
        value is YarnLiteral yarn ? ResolveUnicodeEscapes(yarn.Value) : value;

    // ==================== String Operations ====================

    /// <summary>
    /// SMOOSH: concatenate all arguments after casting each to YARN.
    /// </summary>
    public static string Smoosh(params object?[] args)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var arg in args)
            sb.Append(CastToYarn(arg));
        return sb.ToString();
    }

    // ==================== I/O ====================

    /// <summary>
    /// VISIBLE: print arguments concatenated as YARN.
    /// </summary>
    public static void Print(object?[] args, bool suppressNewline)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var arg in args)
            sb.Append(CastToYarn(arg));

        if (suppressNewline)
            Console.Write(sb.ToString());
        else
            Console.WriteLine(sb.ToString());
    }

    /// <summary>
    /// GIMMEH: read a line of input.
    /// </summary>
    public static string ReadLine()
    {
        return Console.ReadLine() ?? "";
    }

    /// <summary>Writes the UTF-8 byte-order mark preserved from source.</summary>
    public static void WriteByteOrderMark() => Console.Write('\uFEFF');
}

/// <summary>
/// Exception thrown for runtime errors in LOLCODE programs.
/// </summary>
public class LolRuntimeException : Exception
{
    public LolRuntimeException(string message) : base(message) { }
    public LolRuntimeException(string message, Exception inner) : base(message, inner) { }
}
