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
            using var galaxy = new GalaxyExports();
            object game = galaxy.CREATEGAME("CAPTAIN", 7);
            galaxy.TRAVEL(game, 1);
            galaxy.MINE(game);

            galaxy.SAVE(game, savePath).Should().Be(true);
            File.ReadAllText(savePath).Should().StartWith("CHG3\n1\n6\n5\n9\n1\n0\n2\n7\n0\n");

            object loaded = galaxy.LOAD(savePath);
            galaxy.STATUS(loaded).Should().Be(galaxy.STATUS(game));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData("BAD3\n99\n")]
    [InlineData("CHG3\n")]
    [InlineData("CHG3\n1\n6\n5\n9\n1\n0\n2\n7\n")]
    public void Load_InvalidOrTruncatedSaveReturnsNoobAsNull(string contents)
    {
        string directory = TestProcess.CreateTemporaryDirectory();
        string savePath = Path.Combine(directory, "bad.save");
        try
        {
            using var galaxy = new GalaxyExports();
            File.WriteAllText(savePath, contents);

            galaxy.LOAD(savePath).Should().BeNull();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
