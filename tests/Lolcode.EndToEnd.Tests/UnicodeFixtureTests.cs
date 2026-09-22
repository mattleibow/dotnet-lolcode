namespace Lolcode.EndToEnd.Tests;

/// <summary>Retains the host-console assertion for the canonical Unicode escape fixture.</summary>
public sealed class UnicodeFixtureTests : EndToEndTestBase
{
    [Fact]
    public void Unicode_hex_escape_non_ascii_round_trips_when_the_host_console_supports_it()
    {
        if (OperatingSystem.IsWindows())
            return;

        AssertOutput(
            FixtureSource("DotNet/1.2/Strings/unicode-hex-escape-non-ascii/test.lol"),
            "SMILE: ☺\nGRIN: 😀");
    }
}
