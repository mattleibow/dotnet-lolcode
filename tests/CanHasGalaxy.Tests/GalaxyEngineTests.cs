using CanHasGalaxy;

namespace CanHasGalaxy.Tests;

public class GalaxyEngineTests
{
    [Fact]
    public void TravelMineAndFight_AreDeterministic()
    {
        using var galaxy = new GalaxyExports();
        object game = galaxy.CREATEGAME("TESTER", 7);

        galaxy.TRAVEL(game, 1).Should().Be(
            "SECTOR 1: NEBULA OF LASER POINTERZ | QUIET STARS. TEH RADIO PURRZ.");
        galaxy.MINE(game).Should().Be("MINED 1 ORE. HOLD IZ 1");
        galaxy.FIGHT(game).Should().Be("WON TEH DOGFIGHT. HULL -1, BOUNTY +3.");

        galaxy.STATUS(game).Should().Be(
            "CAPTAIN TESTER | T3 | S1 | C9 | F5 | H8 | ORE 1");
    }

    [Fact]
    public void InvalidTravel_LeavesStateUnchanged()
    {
        using var galaxy = new GalaxyExports();
        object game = galaxy.CREATEGAME("TESTER", 7);

        galaxy.TRAVEL(game, 99).Should().Be("STAR CHART DOES NOT GO THERE.");
        ((string)galaxy.STATUS(game)).Should().Contain("| T0 | S0 |");
    }
}
