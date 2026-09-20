using TerminalUi;

namespace CanHasGalaxy.Tests;

public class TerminalUiTests
{
    private static string Yarn(object value) =>
        value is string text
            ? text
            : System.Text.Encoding.UTF8.GetString(
                (byte[])value.GetType().GetProperty("Bytes")!.GetValue(value)!);

    [Fact]
    public void TextWidgets_ExposeStableGeneratedWrappers()
    {
        UiExports.REPEAT("-", 4).Should().Be("----");
        UiExports.REPEAT("-", -1).Should().Be(string.Empty);
        UiExports.PADRIGHT("X", 3).Should().Be("X  ");
        UiExports.PADRIGHT("GALAXY", 3).Should().Be("GALAXY");
        Yarn(UiExports.TRUNCATE("GALAXY", 3)).Should().Be("GAL");
        Yarn(UiExports.FIT("CAT", 6)).Should().Be("CAT   ");
        Yarn(UiExports.FIT("CATACOMBS", 3)).Should().Be("CAT");
        UiExports.BOXLINE("GALAXY").Should().Be("| GALAXY |");
    }

    [Fact]
    public void StatusBar_ClampsInvalidDimensions()
    {
        UiExports.STATUSBAR("HULL", 12, 9).Should().Be("HULL [#########] 9/9");
        UiExports.STATUSBAR("HULL", 4, -1).Should().Be("HULL [] 0/0");
    }

    [Fact]
    public void RetainedViews_ComposeFixedWidthUnicodePanels()
    {
        object left = UiExports.NEWVIEW(8);
        UiExports.ADDLINE(left, "STAR");
        UiExports.ADDLINE(left, "ABCDEFGHIJK");

        object right = UiExports.NEWVIEW(6);
        UiExports.ADDLINE(right, "HULL");

        object framed = UiExports.FRAME(left, " MAP ");
        string frame = Yarn(UiExports.TOTEXT(framed));
        frame.Should().Contain("┌");
        frame.Should().Contain("└");
        frame.Should().Contain("STAR");
        Yarn(UiExports.GETLINE(left, 1)).Should().Be("ABCDEFGH");

        object stacked = UiExports.HSTACK(framed, right, 2);
        string firstLine = Yarn(UiExports.GETLINE(stacked, 0));
        firstLine.Should().Contain("┌");
        UiExports.VIEWWIDTH(stacked).Should().Be(20);
        UiExports.VIEWCOUNT(stacked).Should().Be(5);
    }

    [Fact]
    public void ProgressAndVerticalComposition_RemainSnapshotFriendly()
    {
        Yarn(UiExports.PROGRESS("FUEL", 5, 9, 24)).Should().Be("FUEL [#####.....] 5/9   ");

        object banner = UiExports.BANNER("KITTEH", 16);
        object message = UiExports.MESSAGE("READY", 16);
        string screen = Yarn(UiExports.TOTEXT(UiExports.VSTACK(banner, message)));

        screen.Split('\n').Should().HaveCount(7);
        screen.Should().Contain("╔");
        screen.Should().Contain("KITTEH");
        screen.Should().Contain("COMMS");
        screen.Should().Contain("READY");
    }
}
