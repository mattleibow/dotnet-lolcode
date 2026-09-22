using Lolcode.Runtime;

[assembly: LolcodeLibraryProvider("STRING", typeof(LoaderFixtures.STRING))]

namespace LoaderFixtures;

public static class STRING
{
    public static int LEN(string value) => 99;
}
