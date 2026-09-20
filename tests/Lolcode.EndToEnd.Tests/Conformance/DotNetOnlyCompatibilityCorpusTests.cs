namespace Lolcode.EndToEnd.Tests;

/// <summary>Runs each classified lci divergence without weakening shared conformance.</summary>
[Collection(nameof(LciConformanceCollection))]
public sealed class KnownLciDivergenceCompatibilityCorpusTests : IDisposable
{
    private readonly DotNetLolcodeEngine _dotnet = new();

    public static IEnumerable<object[]> RegisteredCases =>
        KnownLciDivergenceCompatibilityCorpus.Registrations.Select(
            (test, index) => new object[] { index, test.Id });

    [Fact]
    public void Registered_inventory_is_not_empty() =>
        KnownLciDivergenceCompatibilityCorpus.Registrations.Should().NotBeEmpty();

    [Theory]
    [MemberData(nameof(RegisteredCases))]
    public async Task Dotnet_lolcode_matches_known_lci_divergence_fixture(int index, string id)
    {
        LciTestRegistration test = KnownLciDivergenceCompatibilityCorpus.Registrations[index];
        test.Id.Should().Be(id);

        ProcessExecution result = await _dotnet.RunAsync(test);
        CompatibilityCorpusTests.AssertFixtureResult(
            "dotnet-lolcode",
            test,
            result,
            requireDiagnosticSubstring: true);
    }

    public void Dispose() => _dotnet.Dispose();
}
