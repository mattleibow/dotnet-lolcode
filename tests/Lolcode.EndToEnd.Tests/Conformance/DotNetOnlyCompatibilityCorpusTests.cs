namespace Lolcode.EndToEnd.Tests;

/// <summary>Runs each classified dotnet-lolcode-only fixture without weakening lci conformance.</summary>
[Collection(nameof(LciConformanceCollection))]
public sealed class DotNetOnlyCompatibilityCorpusTests : IDisposable
{
    private readonly DotNetLolcodeEngine _dotnet = new();

    public static IEnumerable<object[]> RegisteredCases =>
        DotNetOnlyCompatibilityCorpus.Registrations.Select(
            (test, index) => new object[] { index, test.Id });

    [Fact]
    public void Registered_inventory_is_not_empty() =>
        DotNetOnlyCompatibilityCorpus.Registrations.Should().NotBeEmpty();

    [Theory]
    [MemberData(nameof(RegisteredCases))]
    public async Task Dotnet_lolcode_matches_dotnet_only_fixture(int index, string id)
    {
        LciTestRegistration test = DotNetOnlyCompatibilityCorpus.Registrations[index];
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
