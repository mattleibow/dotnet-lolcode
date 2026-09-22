using System.Globalization;
using System.Text;

namespace Lolcode.Runtime;

public static partial class LolRuntime
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
            LolByteYarn yarn => yarn.Bytes.Length > 0,
            LolObject => throw new LolRuntimeException("Cannot cast BUKKIT to TROOF"),
            LolFunction => throw new LolRuntimeException("Cannot cast function to TROOF"),
            LolBlob => throw new LolRuntimeException("Cannot cast BLOB to TROOF"),
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
            LolByteYarn yarn => ParseNumbrPrefix(System.Text.Encoding.Latin1.GetString(yarn.Bytes)),
            LolObject => throw new LolRuntimeException("Cannot cast BUKKIT to NUMBR"),
            LolFunction => throw new LolRuntimeException("Cannot cast function to NUMBR"),
            LolBlob => throw new LolRuntimeException("Cannot cast BLOB to NUMBR"),
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
            LolByteYarn yarn => ParseNumbarPrefix(System.Text.Encoding.Latin1.GetString(yarn.Bytes)),
            LolObject => throw new LolRuntimeException("Cannot cast BUKKIT to NUMBAR"),
            LolFunction => throw new LolRuntimeException("Cannot cast function to NUMBAR"),
            LolBlob => throw new LolRuntimeException("Cannot cast BLOB to NUMBAR"),
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
            LolByteYarn yarn => System.Text.Encoding.Latin1.GetString(yarn.Bytes),
            LolObject => throw new LolRuntimeException("Cannot cast BUKKIT to YARN"),
            LolFunction => throw new LolRuntimeException("Cannot cast function to YARN"),
            LolBlob => throw new LolRuntimeException("Cannot cast BLOB to YARN"),
            _ => value.ToString() ?? ""
        };
    }

    /// <summary>Creates a source YARN whose Unicode escapes resolve when the value is used.</summary>
    public static object CreateYarnLiteral(string value) => new YarnLiteral(value);

    /// <summary>Creates a YARN that preserves its original UTF-8 byte representation.</summary>
    public static object CreateByteYarn(byte value) => new LolByteYarn([value]);

    /// <summary>Creates a YARN that preserves the supplied UTF-8 byte representation.</summary>
    public static object CreateByteYarn(byte[] bytes) =>
        new LolByteYarn((byte[])bytes.Clone());

    /// <summary>Gets a YARN's UTF-8 bytes without normalizing byte-preserving YARN values.</summary>
    public static byte[] GetYarnBytes(object? value) =>
        value is LolByteYarn yarn
            ? yarn.Bytes
            : Encoding.UTF8.GetBytes(CastToYarn(value));

    /// <summary>Gets the UTF-8 bytes of a value after explicit YARN coercion.</summary>
    public static byte[] GetExplicitYarnBytes(object? value) =>
        value is null
            ? []
            : value is LolByteYarn yarn
                ? yarn.Bytes
                : GetYarnBytes(ExplicitCast(value, "YARN"));

    /// <summary>
    /// Builds a source YARN by resolving its parsed interpolation names in the active scope.
    /// This compatibility API represents byte-backed YARNs as Latin-1 text.
    /// </summary>
    public static string InterpolateYarn(LolScope scope, string[] textParts, string[] names)
        => CastToYarn(InterpolateYarnValue(scope, textParts, names));

    /// <summary>
    /// Builds a source YARN while preserving byte-backed interpolated values.
    /// </summary>
    public static object InterpolateYarnValue(LolScope scope, string[] textParts, string[] names)
    {
        if (textParts.Length != names.Length + 1)
            throw new ArgumentException("Interpolation text and name counts do not match.");

        var values = new object?[textParts.Length + names.Length];
        for (int index = 0; index < names.Length; index++)
        {
            values[index * 2] = ResolveUnicodeEscapes(textParts[index]);
            values[(index * 2) + 1] =
                names[index] == "IT" ? scope.It : Lookup(scope, names[index]);
        }
        values[^1] = ResolveUnicodeEscapes(textParts[^1]);
        return ConcatenateYarns(values);
    }

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
            "YARN" when value is LolByteYarn => value,
            "YARN" => (object)CastToYarn(value),
            "NOOB" => null,
            _ => throw new InvalidOperationException($"Unknown type: {targetType}")
        };
    }
}
