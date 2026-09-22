
using System.Text;

namespace Lolcode.Runtime;

public static partial class LolRuntime
{
    // ==================== String Operations ====================

    /// <summary>
    /// SMOOSH: concatenate all arguments after casting each to YARN.
    /// </summary>
    public static string Smoosh(params object?[] args) => CastToYarn(SmooshValue(args));

    /// <summary>SMOOSH preserving the exact bytes of byte-backed YARN operands.</summary>
    public static object SmooshValue(params object?[] args) => ConcatenateYarns(args);

    private static object ConcatenateYarns(object?[] values)
    {
        if (!values.Any(static value => value is LolByteYarn))
        {
            var text = new StringBuilder();
            foreach (object? value in values)
                text.Append(CastToYarn(value));
            return text.ToString();
        }

        var bytes = new List<byte>();
        foreach (object? value in values)
            bytes.AddRange(GetYarnBytes(value));
        return new LolByteYarn([.. bytes]);
    }
}
