using CanHasGalaxy;

namespace CanHasGalaxy.Tests;

public class GalaxyPersistenceTests
{
    [Fact]
    public void SaveAndLoad_RoundTripsDeterministicPrimitives()
    {
        string directory = TestProcess.CreateTemporaryDirectory();
        string savePath = Path.Combine(directory, "galaxy.save");
        try
        {
            object game = GalaxyExports.CREATEGAME("CAPTAIN", 7);
            GalaxyExports.TRAVEL(game, 1);
            GalaxyExports.MINE(game);

            GalaxyExports.SAVE(game, savePath).Should().Be(true);
            File.ReadAllText(savePath).Should().StartWith("CHG3\n1\n6\n5\n9\n1\n0\n2\n7\n0\n");

            object loaded = GalaxyExports.LOAD(savePath);
            GalaxyExports.STATUS(loaded).Should().Be(GalaxyExports.STATUS(game));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Load_InvalidSaveReturnsNoobAsNull()
    {
        string directory = TestProcess.CreateTemporaryDirectory();
        string savePath = Path.Combine(directory, "bad.save");
        try
        {
            File.WriteAllText(savePath, "BAD3\n99\n");

            GalaxyExports.LOAD(savePath).Should().BeNull();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
