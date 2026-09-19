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
            result.StdOut.Should().Contain("CAN HAS GALAXY?  SPACE TRADER");
            result.StdOut.Should().Contain("SAVE OK: can-has-galaxy.save");
            result.StdOut.Should().Contain("LOAD OK.");
            result.StdOut.Should().Contain("CAPTAIN CAPTAIN | T2 | S1 | C6 | F5 | H9 | ORE 1");
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
            result.StdOut.Should().Contain("| KITTEH CATACOMBS |");
            result.StdOut.Should().Contain("COMMANDS: LOOK STEP QUIT");
            result.StdOut.Should().Contain("CATACOMBS COMPLETE.");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
