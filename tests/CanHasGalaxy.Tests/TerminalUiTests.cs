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
    public void TextWidgets_ExposeStableGeneratedExports()
    {
        using var ui = new UiExports();
        ui.REPEAT("-", 4).Should().Be("----");
        ui.REPEAT("-", -1).Should().Be(string.Empty);
        ui.PADRIGHT("X", 3).Should().Be("X  ");
        ui.PADRIGHT("GALAXY", 3).Should().Be("GALAXY");
        Yarn(ui.TRUNCATE("GALAXY", 3)).Should().Be("GAL");
        Yarn(ui.FIT("CAT", 6)).Should().Be("CAT   ");
        Yarn(ui.FIT("CATACOMBS", 3)).Should().Be("CAT");
        ui.BOXLINE("GALAXY").Should().Be("| GALAXY |");
    }

    [Fact]
    public void StatusBar_ClampsInvalidDimensions()
    {
        using var ui = new UiExports();
        ui.STATUSBAR("HULL", 12, 9).Should().Be("HULL [#########] 9/9");
        ui.STATUSBAR("HULL", 4, -1).Should().Be("HULL [] 0/0");
    }

    [Fact]
    public void RetainedViews_ComposeFixedWidthUnicodePanels()
    {
        using var ui = new UiExports();
        object left = ui.NEWVIEW(8);
        ui.ADDLINE(left, "STAR");
        ui.ADDLINE(left, "ABCDEFGHIJK");

        object right = ui.NEWVIEW(6);
        ui.ADDLINE(right, "HULL");

        object framed = ui.FRAME(left, " MAP ");
        string frame = Yarn(ui.TOTEXT(framed));
        frame.Should().Contain("┌");
        frame.Should().Contain("└");
        frame.Should().Contain("STAR");
        Yarn(ui.GETLINE(left, 1)).Should().Be("ABCDEFGH");

        object stacked = ui.HSTACK(framed, right, 2);
        string firstLine = Yarn(ui.GETLINE(stacked, 0));
        firstLine.Should().Contain("┌");
        ui.VIEWWIDTH(stacked).Should().Be(20);
        ui.VIEWCOUNT(stacked).Should().Be(5);
    }

    [Fact]
    public void ProgressAndVerticalComposition_RemainSnapshotFriendly()
    {
        using var ui = new UiExports();
        Yarn(ui.PROGRESS("FUEL", 5, 9, 24)).Should().Be("FUEL [#####.....] 5/9   ");

        object banner = ui.BANNER("KITTEH", 16);
        object message = ui.MESSAGE("READY", 16);
        string screen = Yarn(ui.TOTEXT(ui.VSTACK(banner, message)));

        screen.Split('\n').Should().HaveCount(7);
        screen.Should().Contain("╔");
        screen.Should().Contain("KITTEH");
        screen.Should().Contain("COMMS");
        screen.Should().Contain("READY");
    }
}
