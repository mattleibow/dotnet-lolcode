using CanHasGalaxy;

namespace CanHasGalaxy.Tests;

public class GalaxyEngineTests
{
    [Fact]
    public void TravelMineAndFight_AreDeterministic()
    {
        object game = GalaxyExports.CREATEGAME("TESTER", 7);

        GalaxyExports.TRAVEL(game, 1).Should().Be(
            "SECTOR 1: NEBULA OF LASER POINTERZ | QUIET STARS. TEH RADIO PURRZ.");
        GalaxyExports.MINE(game).Should().Be("MINED 1 ORE. HOLD IZ 1");
        GalaxyExports.FIGHT(game).Should().Be("WON TEH DOGFIGHT. HULL -1, BOUNTY +3.");

        GalaxyExports.STATUS(game).Should().Be(
            "CAPTAIN TESTER | T3 | S1 | C9 | F5 | H8 | ORE 1");
    }

    [Fact]
    public void InvalidTravel_LeavesStateUnchanged()
    {
        object game = GalaxyExports.CREATEGAME("TESTER", 7);

        GalaxyExports.TRAVEL(game, 99).Should().Be("STAR CHART DOES NOT GO THERE.");
        ((string)GalaxyExports.STATUS(game)).Should().Contain("| T0 | S0 |");
    }
}
