[assembly: Lolcode.Runtime.LolcodeModule("CUSTOM", typeof(CustomProviderFixture.CustomLibrary))]

namespace CustomProviderFixture;

public static class CustomLibrary
{
    public static string ECHO(string value) => $"CUSTOM {value}";
}
