using Lolcode.Runtime;

#pragma warning disable CS8625
[assembly: LolcodeLibraryProvider(null, typeof(InvalidProviderFixture.InvalidLibrary))]
#pragma warning restore CS8625
[assembly: LolcodeLibraryProvider(
    "OLD",
    typeof(InvalidProviderFixture.InvalidLibrary),
    contractVersion: 2)]

namespace InvalidProviderFixture;

public static class InvalidLibrary
{
    public static string ECHO(string value) => value;
}
