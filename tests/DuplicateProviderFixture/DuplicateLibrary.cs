using Lolcode.Runtime;

[assembly: LolcodeLibraryProvider("CUSTOM", typeof(DuplicateProviderFixture.DuplicateLibrary))]

namespace DuplicateProviderFixture;

public static class DuplicateLibrary
{
    public static string ECHO(string value) => value;
}
