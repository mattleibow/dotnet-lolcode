using System.Diagnostics;

namespace Lolcode.EndToEnd.Tests;

/// <summary>Runs every registered upstream lci fixture using the pinned native lci executable.</summary>
[Collection(nameof(LciConformanceCollection))]
public sealed class PinnedLciConformanceTests
{
    public static IEnumerable<object[]> RegisteredCases =>
        LciConformanceCorpus.Registrations.Select(
            (test, index) => new object[] { index, test.Id });

    [LciTheory]
    [MemberData(nameof(RegisteredCases))]
    public async Task Pinned_lci_matches_upstream_registration(int index, string id)
    {
        LciTestRegistration test = LciConformanceCorpus.Registrations[index];
        test.Id.Should().Be(id);

        string lciPath = Environment.GetEnvironmentVariable("LCI_PATH")
            ?? throw new InvalidOperationException("LCI_PATH was not configured for a pinned lci test.");
        var startInfo = new ProcessStartInfo
        {
            FileName = lciPath,
            WorkingDirectory = test.WorkingDirectoryPath ?? Path.GetDirectoryName(test.SourcePath)!,
        };
        startInfo.ArgumentList.Add(test.SourcePath);

        ProcessExecution result = await CompatibilityProcessRunner.RunAsync(
            startInfo,
            test.InputPath is null ? null : DotNetLolcodeEngine.ReadUtf8(test.InputPath),
            TimeSpan.FromSeconds(30));
        CompatibilityCorpusTests.AssertFixtureResult(
            "pinned lci",
            test,
            result,
            validateDotNetPhase: false);
    }
}
