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
        AssertFixtureResult("dotnet-lolcode", test, result, validateDotNetPhase: true);
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
        AssertFixtureResult("pinned lci", test, result, validateDotNetPhase: false);
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
        bool validateDotNetPhase)
    {
        string source = DotNetLolcodeEngine.ReadUtf8(test.SourcePath);
        string details =
            $"Case: {test.Id}{Environment.NewLine}" +
            $"Source: {test.SourcePath}{Environment.NewLine}" +
            $"Engine: {engine}{Environment.NewLine}" +
            $"Phase: {(result.ProcessLaunched ? "runtime" : "compilation")}{Environment.NewLine}" +
            $"Exit code: {result.ExitCode}{Environment.NewLine}" +
            $"stdout:{Environment.NewLine}{result.StandardOutputText}{Environment.NewLine}" +
            $"stderr:{Environment.NewLine}{result.StandardErrorText}{Environment.NewLine}" +
            $"diagnostics:{Environment.NewLine}{result.CompilationDiagnosticText}{Environment.NewLine}" +
            $"source:{Environment.NewLine}{source}";

        if (test.ExpectedErrorPath is not null)
        {
            if (validateDotNetPhase)
                result.ProcessLaunched.Should().BeTrue(
                    $"{details}{Environment.NewLine}test.err requires successful compilation and a launched program.");

            result.ExitCode.Should().NotBe(0, details);
            if (validateDotNetPhase)
            {
                string expectedDiagnostic = DotNetLolcodeEngine.ReadUtf8(test.ExpectedErrorPath).Trim();
                result.StandardErrorText.Should().Contain(expectedDiagnostic, details);
            }

            if (test.ExpectedOutputPath is not null)
            {
                byte[] expectedOutput = NormalizeLineEndings(File.ReadAllBytes(test.ExpectedOutputPath));
                NormalizeLineEndings(result.StandardOutput).Should().Equal(expectedOutput, details);
            }

            return;
        }

        if (test.ExpectedDiagnosticPath is not null)
        {
            if (validateDotNetPhase)
            {
                result.ProcessLaunched.Should().BeFalse(
                    $"{details}{Environment.NewLine}test.diag requires compiler diagnostics, not a launched process.");
                DiagnosticExpectation expectation = DiagnosticExpectation.Parse(
                    DotNetLolcodeEngine.ReadUtf8(test.ExpectedDiagnosticPath));
                result.CompilationDiagnostics.Select(diagnostic => diagnostic.Id)
                    .Should()
                    .Contain(expectation.Id, details);

                if (expectation.Message is not null)
                {
                    result.CompilationDiagnostics
                        .Where(diagnostic => diagnostic.Id == expectation.Id)
                        .Select(diagnostic => diagnostic.Message)
                        .Should()
                        .Contain(message => message.Contains(expectation.Message, StringComparison.Ordinal), details);
                }

                if (expectation.Location is not null)
                {
                    result.CompilationDiagnostics
                        .Where(diagnostic => diagnostic.Id == expectation.Id)
                        .Select(diagnostic => diagnostic.Location.ToString())
                        .Should()
                        .Contain(expectation.Location, details);
                }
            }
            else
            {
                result.ExitCode.Should().NotBe(0, details);
            }

            if (test.ExpectedOutputPath is not null)
            {
                byte[] expectedOutput = NormalizeLineEndings(File.ReadAllBytes(test.ExpectedOutputPath));
                NormalizeLineEndings(result.StandardOutput).Should().Equal(expectedOutput, details);
            }

            return;
        }

        if (test.ExpectError)
        {
            result.ExitCode.Should().NotBe(0, details);
            return;
        }

        if (validateDotNetPhase)
            result.ProcessLaunched.Should().BeTrue(details);
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

    private sealed record DiagnosticExpectation(string Id, string? Message, string? Location)
    {
        internal static DiagnosticExpectation Parse(string contents)
        {
            string[] lines = contents.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (lines.Length == 0 || !System.Text.RegularExpressions.Regex.IsMatch(lines[0], "^LOL[0-9]{4}$"))
                throw new InvalidDataException("test.diag must begin with an exact LOL diagnostic ID.");

            string? message = null;
            string? location = null;
            foreach (string line in lines.Skip(1))
            {
                int separator = line.IndexOf(':');
                if (separator <= 0)
                    throw new InvalidDataException(
                        "test.diag optional expectations must use `message:` or `location:`.");

                string key = line[..separator].Trim();
                string value = line[(separator + 1)..].Trim();
                switch (key)
                {
                    case "message":
                        message ??= value;
                        break;
                    case "location":
                        location ??= value;
                        break;
                    default:
                        throw new InvalidDataException(
                            $"Unsupported test.diag expectation '{key}'.");
                }
            }

            return new DiagnosticExpectation(lines[0], message, location);
        }
    }
}
