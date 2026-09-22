namespace Lolcode.Runtime;

[LolcodeLibrary("STRING")]
public sealed class StringLibrary
{
    /// <summary>Gets the byte length of a YARN.</summary>
    public int LEN(object value) => LolRuntime.GetYarnBytes(value).Length;

    /// <summary>Gets a byte at a YARN position.</summary>
    public object AT(object value, int position)
    {
        byte[] bytes = LolRuntime.GetYarnBytes(value);
        return position < 0 || position >= bytes.Length
            ? string.Empty
            : LolRuntime.CreateByteYarn(bytes[position]);
    }
}
