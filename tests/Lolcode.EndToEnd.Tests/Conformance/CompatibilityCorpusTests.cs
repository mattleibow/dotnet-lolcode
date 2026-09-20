using System.Diagnostics;
using System.Text;

namespace Lolcode.EndToEnd.Tests;

/// <summary>Runs every repository-owned lci-format fixture with dotnet-lolcode and pinned lci.</summary>
[Collection(nameof(LciConformanceCollection))]
public sealed class CompatibilityCorpusTests : IDisposable
{
    private readonly DotNetLolcodeEngine _dotnet = new();

    public static IEnumerable<object[]> RegisteredCases =>
        CompatibilityCorpus.Registrations.Select(
            (test, index) => new object[] { index, test.Id });

    [Fact]
    public void Registered_inventory_is_not_empty() =>
        CompatibilityCorpus.Registrations.Should().NotBeEmpty();

    [Theory]
    [MemberData(nameof(RegisteredCases))]
    public async Task Dotnet_lolcode_matches_registered_fixture(int index, string id)
    {
        LciTestRegistration test = GetTest(index, id);
        ProcessExecution result = await _dotnet.RunAsync(test);
        AssertFixtureResult("dotnet-lolcode", test, result, requireDiagnosticSubstring: true);
    }

    [LciTheory]
    [MemberData(nameof(RegisteredCases))]
    public async Task Pinned_lci_matches_registered_fixture(int index, string id)
    {
        _ = TryGetLciPath(out string? lciPath);
        LciTestRegistration test = GetTest(index, id);
        var startInfo = new ProcessStartInfo
        {
            FileName = lciPath!,
            WorkingDirectory = test.WorkingDirectoryPath ?? Path.GetDirectoryName(test.SourcePath)!,
        };
        startInfo.ArgumentList.Add(test.SourcePath);
        ProcessExecution result = await CompatibilityProcessRunner.RunAsync(
            startInfo,
            test.InputPath is null ? null : DotNetLolcodeEngine.ReadUtf8(test.InputPath),
            TimeSpan.FromSeconds(30));
        AssertFixtureResult("pinned lci", test, result, requireDiagnosticSubstring: false);
    }

    public void Dispose() => _dotnet.Dispose();

    private static LciTestRegistration GetTest(int index, string id)
    {
        LciTestRegistration test = CompatibilityCorpus.Registrations[index];
        test.Id.Should().Be(id);
        return test;
    }

    internal static void AssertFixtureResult(
        string engine,
        LciTestRegistration test,
        ProcessExecution result,
        bool requireDiagnosticSubstring)
    {
        string source = DotNetLolcodeEngine.ReadUtf8(test.SourcePath);
        string details =
            $"Case: {test.Id}{Environment.NewLine}" +
            $"Source: {test.SourcePath}{Environment.NewLine}" +
            $"Engine: {engine}{Environment.NewLine}" +
            $"Exit code: {result.ExitCode}{Environment.NewLine}" +
            $"stdout:{Environment.NewLine}{result.StandardOutputText}{Environment.NewLine}" +
            $"stderr:{Environment.NewLine}{result.StandardErrorText}{Environment.NewLine}" +
            $"source:{Environment.NewLine}{source}";

        if (test.ExpectError)
        {
            result.ExitCode.Should().NotBe(0, details);
            if (requireDiagnosticSubstring && test.ExpectedErrorPath is not null)
            {
                string expectedDiagnostic = DotNetLolcodeEngine.ReadUtf8(test.ExpectedErrorPath).Trim();
                result.StandardErrorText.Should().Contain(expectedDiagnostic, details);
            }

            return;
        }

        result.ExitCode.Should().Be(0, details);
        byte[] expected = NormalizeLineEndings(File.ReadAllBytes(test.ExpectedOutputPath!));
        NormalizeLineEndings(result.StandardOutput).Should().Equal(expected, details);
    }

    [Fact]
    public void Pinned_lci_is_required_only_when_requested()
    {
        if (Environment.GetEnvironmentVariable("REQUIRE_LCI") == "1")
        {
            TryGetLciPath(out _).Should().BeTrue(
                "CI must set LCI_PATH to the pinned lci executable.");
        }
    }

    private static bool TryGetLciPath(out string? path)
    {
        path = Environment.GetEnvironmentVariable("LCI_PATH");
        return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
    }

    internal static byte[] NormalizeLineEndings(byte[] value)
    {
        int firstCarriageReturn = Array.IndexOf(value, (byte)'\r');
        if (firstCarriageReturn < 0)
            return value;

        using var normalized = new MemoryStream(value.Length);
        normalized.Write(value, 0, firstCarriageReturn);
        for (int index = firstCarriageReturn; index < value.Length; index++)
        {
            if (value[index] == '\r' && index + 1 < value.Length && value[index + 1] == '\n')
                continue;

            normalized.WriteByte(value[index]);
        }

        return normalized.ToArray();
    }
}
