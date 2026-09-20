using Lolcode.Compatibility.Flatten;

namespace Lolcode.EndToEnd.Tests;

/// <summary>Verifies semantic project flattening fixtures and rejection behavior.</summary>
public sealed class FixtureFlattenerTests
{
    [Theory]
    [InlineData("caller-before-callee-1.2")]
    [InlineData("caller-before-callee-1.3")]
    [InlineData("caller-before-callee-1.4")]
    [InlineData("comma-after-function")]
    [InlineData("comma-before-function")]
    [InlineData("inline-wrapper")]
    [InlineData("initializer-call")]
    [InlineData("function-replacement")]
    [InlineData("mutual-recursion")]
    [InlineData("state-order")]
    public void Generated_fixture_matches_canonical_sources(string caseName)
    {
        string directory = FlattenFixtureDirectory(caseName);
        FixtureFlattener.Flatten(FixtureFlattener.ReadManifest(directory))
            .Should()
            .Be(File.ReadAllText(Path.Combine(directory, "test.lol")));
    }

    [Fact]
    public void Generated_fixture_comparison_accepts_CRLF_without_changing_generated_LF()
    {
        string directory = FlattenFixtureDirectory("inline-wrapper");
        string generated = FixtureFlattener.Flatten(FixtureFlattener.ReadManifest(directory));
        string crlfFixture = generated.Replace("\n", "\r\n", StringComparison.Ordinal);

        FixtureFlattener.NormalizeLineEndings(crlfFixture).Should().Be(generated);
        generated.Should().NotContain("\r");
    }

    [Fact]
    public void Dynamic_function_is_not_hoisted()
    {
        string directory = Path.Combine(
            AppContext.BaseDirectory,
            "Compatibility",
            "DotNetOnly",
            "ProjectFlatten",
            "dynamic-srs-not-hoisted");
        string source = FixtureFlattener.Flatten(FixtureFlattener.ReadManifest(directory));

        source.IndexOf("VISIBLE \"dynamic declaration", StringComparison.Ordinal)
            .Should()
            .BeLessThan(source.IndexOf("HOW IZ I SRS name", StringComparison.Ordinal));
    }

    [Fact]
    public void Mismatched_versions_are_rejected()
    {
        string directory = Path.Combine(
            AppContext.BaseDirectory,
            "Compatibility",
            "DotNetOnly",
            "ProjectFlatten",
            "version-mismatch-rejected");
        string[] paths = File.ReadAllLines(Path.Combine(directory, "mismatch.sources.txt"))
            .Select(path => Path.Combine(directory, path))
            .ToArray();

        Action flatten = () => FixtureFlattener.Flatten(paths);
        flatten.Should().Throw<InvalidDataException>().WithMessage("*HAI 1.2*");
    }

    private static string FlattenFixtureDirectory(string caseName) =>
        Path.Combine(
            AppContext.BaseDirectory,
            "Compatibility",
            "ProjectFlatten",
            caseName);
}
