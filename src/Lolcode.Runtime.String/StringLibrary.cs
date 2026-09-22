namespace Lolcode.Runtime.String;

internal static class StringLibrary
{
    public static int LEN(object value) => LolRuntime.GetYarnBytes(value).Length;

    public static object AT(object value, int position)
    {
        byte[] bytes = LolRuntime.GetYarnBytes(value);
        return position < 0 || position >= bytes.Length
            ? string.Empty
            : LolRuntime.CreateByteYarn(bytes[position]);
    }
}
