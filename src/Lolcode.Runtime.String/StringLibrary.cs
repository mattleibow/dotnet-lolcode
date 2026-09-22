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

/// <summary>Creates the official STRING provider for static LOLCODE imports.</summary>
public static class StringLibraryFactory
{
    /// <summary>Creates a scope-bound STRING module without runtime discovery.</summary>
    /// <param name="scope">The importing LOLCODE scope.</param>
    /// <returns>The STRING module.</returns>
    public static LolObject Create(LolScope scope)
    {
        var builder = new LolcodeLibraryBuilder(scope);
        builder.AddFunction("LEN", 1, static (_, arguments) => StringLibrary.LEN(arguments[0]!));
        builder.AddFunction("AT", 2, static (_, arguments) =>
            StringLibrary.AT(arguments[0]!, LolRuntime.CastToNumbr(arguments[1])));
        return builder.Build();
    }
}
