using Lolcode.Web.Samples;

namespace Lolcode.Web.Tests;

public sealed class PlaygroundSamplesTests
{
    [Fact]
    public void Filter_BlankQueryReturnsAllSamples()
    {
        PlaygroundSamples.Filter(" ")
            .Should().Equal(PlaygroundSamples.All);
    }

    [Theory]
    [InlineData("fizz", "fizzbuzz")]
    [InlineData("functions", "fibonacci")]
    [InlineData("undeclared", "diagnostics")]
    [InlineData("DIAGNOSTICS", "diagnostics", "runtime")]
    public void Filter_MatchesNameCategoryAndDescription(
        string query,
        params string[] expectedIds)
    {
        PlaygroundSamples.Filter(query)
            .Select(sample => sample.Id)
            .Should().Equal(expectedIds);
    }

    [Fact]
    public void Samples_PreserveExpectedSourceAndInput()
    {
        PlaygroundSamples.All.Should().HaveCount(5);
        PlaygroundSamples.All[0].StandardInput.Should().Be("LOLCAT\n");
        PlaygroundSamples.All.Should().OnlyContain(
            sample => sample.Source.StartsWith("HAI 1.2", StringComparison.Ordinal)
                && sample.Source.EndsWith("KTHXBYE", StringComparison.Ordinal));
    }
}
