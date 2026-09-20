using System.Text.RegularExpressions;

namespace Lolcode.EndToEnd.Tests;

/// <summary>Validates the self-contained repository compatibility fixture convention.</summary>
public sealed partial class CompatibilityFixtureValidationTests
{
    private static readonly string FixtureRoot = Path.Combine(AppContext.BaseDirectory, "Compatibility");
    private static readonly string[] CanonicalRoots = ["Shared", "KnownLciDivergence", "DotNet"];

    [Fact]
    public void Fixture_tree_uses_only_canonical_classification_roots()
    {
        Directory.EnumerateDirectories(FixtureRoot)
            .Select(Path.GetFileName)
            .Should()
            .BeEquivalentTo(CanonicalRoots);

        foreach (string fixture in FixtureDirectories())
        {
            fixture.Split(Path.DirectorySeparatorChar)[0]
                .Should()
                .BeOneOf(CanonicalRoots);
        }
    }

    [Fact]
    public void Lci_compatible_fixtures_have_active_registrations_and_declared_files()
    {
        foreach (string rootName in new[] { "Shared", "KnownLciDivergence" })
        {
            string root = Path.Combine(FixtureRoot, rootName);
            IReadOnlyList<LciTestRegistration> registrations = LciRegistrationParser.Discover(
                root, rootName);
            var registeredDirectories = registrations
                .Select(test => Path.GetDirectoryName(test.SourcePath)!)
                .ToHashSet(StringComparer.Ordinal);

            foreach (string directory in Directory.EnumerateFiles(root, "test.lol", SearchOption.AllDirectories)
                         .Select(Path.GetDirectoryName)!)
            {
                string cmake = Path.Combine(directory, "CMakeLists.txt");
                File.Exists(cmake).Should().BeTrue(
                    $"{Relative(directory)} must declare its lci-compatible registration locally");
                registeredDirectories.Should().Contain(directory,
                    $"{Relative(directory)} must have an active, non-commented ADD_LOL_TEST registration");
            }

            foreach (LciTestRegistration registration in registrations)
            {
                File.Exists(registration.SourcePath).Should().BeTrue();
                if (registration.ExpectedOutputPath is not null)
                    File.Exists(registration.ExpectedOutputPath).Should().BeTrue();
                if (registration.InputPath is not null)
                    File.Exists(registration.InputPath).Should().BeTrue();
                if (registration.ExpectedErrorPath is not null)
                    File.Exists(registration.ExpectedErrorPath).Should().BeTrue();
                if (registration.ExpectedDiagnosticPath is not null)
                    File.Exists(registration.ExpectedDiagnosticPath).Should().BeTrue();
            }
        }
    }

    [Fact]
    public void Divergence_fixtures_carry_their_own_pinned_lci_evidence()
    {
        string root = Path.Combine(FixtureRoot, "KnownLciDivergence");
        foreach (string source in Directory.EnumerateFiles(root, "test.lol", SearchOption.AllDirectories))
        {
            string evidence = Path.Combine(Path.GetDirectoryName(source)!, "README.md");
            File.Exists(evidence).Should().BeTrue($"{Relative(source)} is a divergence and needs local evidence");
            File.ReadAllText(evidence).Should().Contain("Pinned-lci evidence:");
        }
    }

    [Fact]
    public void Diagnostic_sidecars_use_structural_diagnostic_ids()
    {
        foreach (string diagnostic in Directory.EnumerateFiles(FixtureRoot, "test.diag", SearchOption.AllDirectories))
        {
            string[] lines = File.ReadAllLines(diagnostic)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToArray();
            lines.Should().NotBeEmpty();
            lines[0].Should().MatchRegex("^LOL[0-9]{4}$");
            lines.Skip(1).Should().OnlyContain(line => OptionalDiagnosticExpectationRegex().IsMatch(line));
        }
    }

    [Fact]
    public void Every_flattened_fixture_is_current_without_a_separate_tool()
    {
        foreach (string manifest in Directory.EnumerateFiles(FixtureRoot, "sources.txt", SearchOption.AllDirectories))
        {
            string directory = Path.GetDirectoryName(manifest)!;
            FixtureFlattener.Flatten(FixtureFlattener.ReadManifest(directory))
                .Should()
                .Be(FixtureFlattener.NormalizeLineEndings(File.ReadAllText(Path.Combine(directory, "test.lol"))),
                    $"{Relative(directory)} must keep its generated lci source alongside canonical source units");
        }
    }

    private static IEnumerable<string> FixtureDirectories() =>
        Directory.EnumerateFiles(FixtureRoot, "test.lol", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(FixtureRoot, Path.GetDirectoryName(path)!));

    private static string Relative(string path) => Path.GetRelativePath(FixtureRoot, path);

    [GeneratedRegex("^(message|location):\\s*.+$", RegexOptions.CultureInvariant)]
    private static partial Regex OptionalDiagnosticExpectationRegex();
}
