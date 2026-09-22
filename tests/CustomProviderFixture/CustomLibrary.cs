namespace CustomProviderFixture;

[Lolcode.Runtime.LolcodeLibrary("CUSTOM")]
public sealed class CustomLibrary
{
    public string ECHO(string value) => $"CUSTOM {value}";

    public int CONTEXT() => 1;
}

[Lolcode.Runtime.LolcodeLibrary("SECOND")]
public sealed class SecondLibrary
{
    public int VALUE() => 2;
}

[Lolcode.Runtime.LolcodeLibrary("PROPERTY_LIBRARY")]
public sealed class PropertyLibrary
{
    public string Library => "ordinary managed property";

    public string ECHO(string value) => value;
}
