using TerminalUi;

namespace CanHasGalaxy.Tests;

public class TerminalUiTests
{
    [Fact]
    public void TextWidgets_ExposeStableGeneratedWrappers()
    {
        UiExports.REPEAT("-", 4).Should().Be("----");
        UiExports.REPEAT("-", -1).Should().Be(string.Empty);
        UiExports.PADRIGHT("X", 3).Should().Be("X  ");
        UiExports.PADRIGHT("GALAXY", 3).Should().Be("GALAXY");
        UiExports.BOXLINE("GALAXY").Should().Be("| GALAXY |");
    }

    [Fact]
    public void StatusBar_ClampsInvalidDimensions()
    {
        UiExports.STATUSBAR("HULL", 12, 9).Should().Be("HULL [#########] 9/9");
        UiExports.STATUSBAR("HULL", 4, -1).Should().Be("HULL [] 0/0");
    }
}
