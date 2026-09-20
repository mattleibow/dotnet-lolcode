namespace CanHasGalaxy.Tests;

public class GameProcessTests
{
    [Fact]
    public void GalaxyCli_HandlesScriptedSaveLoadAndEof()
    {
        string directory = TestProcess.CreateTemporaryDirectory();
        try
        {
            var result = TestProcess.Run(
                TestProcess.AssemblyPath("CanHasGalaxy.Cli", "CanHasGalaxy.Cli"),
                directory,
                "TRAVEL1\nMINE\nSAVE\nFIGHT\nLOAD\nSTATUS\nQUIT\n");

            result.ExitCode.Should().Be(0, result.StdErr);
            result.StdOut.Should().Contain("CAN HAS GALAXY? / SPACE TRADER");
            result.StdOut.Should().Contain("╔");
            result.StdOut.Should().Contain("┌");
            result.StdOut.Should().Contain("GALACTIC MAP");
            result.StdOut.Should().Contain("SHIP STATUS");
            result.StdOut.Should().Contain("COMMS");
            result.StdOut.Should().Contain("COMMAND DECK");
            result.StdOut.Should().Contain("HULL [");
            result.StdOut.Should().Contain("MINE SELL FUEL FIGHT MISSION SAVE LOAD QUIT");
            result.StdOut.Should().Contain("SAVE OK: can-has-galaxy.save");
            result.StdOut.Should().Contain("LOAD OK.");
            result.StdOut.Should().Contain("CAPTAIN CAPTAIN | T2 | S1 | C6 | F5 | H9 | ORE 1");
            result.StdOut.Should().Contain("KTHXBAI, CAPTAIN.");
            result.StdOut.Replace("\r\n", "\n").Should().NotContain("GALAXY> ==");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Catacombs_UsesSharedRouterAndWidgets()
    {
        string directory = TestProcess.CreateTemporaryDirectory();
        try
        {
            var result = TestProcess.Run(
                TestProcess.AssemblyPath("Kitteh.Catacombs", "Kitteh.Catacombs"),
                directory,
                "LOOK\nSTEP\nSTEP\n");

            result.ExitCode.Should().Be(0, result.StdErr);
            result.StdOut.Should().Contain("KITTEH CATACOMBS / TUNA RUN");
            result.StdOut.Should().Contain("TUNA GATE");
            result.StdOut.Should().Contain("CRUMBLY BRIDGE");
            result.StdOut.Should().Contain("ADVENTURE");
            result.StdOut.Should().Contain("DEPTH [");
            result.StdOut.Should().Contain("COMMAND DECK");
            result.StdOut.Should().Contain("LOOK STEP QUIT");
            result.StdOut.Should().Contain("CATACOMBS COMPLETE.");
            result.StdOut.Replace("\r\n", "\n").Should().NotContain("CAT> ==");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
