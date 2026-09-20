using System.Text;

namespace Lolcode.EndToEnd.Tests;

/// <summary>End-to-end coverage for pinned lci/future language behavior.</summary>
public class FutureFeatureTests : EndToEndTestBase
{
    [Fact]
    public void StdioAppendAndErrorsMatchCStreamSemantics()
    {
        string path = Path.Combine(TestDirectory, "append.dat");
        File.WriteAllText(path, "A");

        AssertOutput(
            FixtureSource("DotNet/1.4/FutureFeature/stdio-append-and-errors-match-c-stream-semantics/test.lol"),
            "AB\nfailed safely");

        File.ReadAllText(path).Should().Be("AB");
    }

    [Fact]
    public void StringAtPreservesBytesThroughAllYarnOperations()
    {
        ExecutionResult result = CompileAndRunWithResult(
            FixtureSource("DotNet/1.4/FutureFeature/string-at-preserves-bytes-through-all-yarn-operations/test.lol"));

        result.ExitCode.Should().Be(0);
        result.StandardOutputBytes.Should().Equal(
            Encoding.UTF8.GetBytes(
                $"2{Environment.NewLine}same{Environment.NewLine}different{Environment.NewLine}" +
                $"cast{Environment.NewLine}switch{Environment.NewLine}é{Environment.NewLine}" +
                $"é{Environment.NewLine}"));
        File.ReadAllBytes(Path.Combine(TestDirectory, "selected.dat")).Should().Equal(0xC3);
    }

    [Fact]
    public void VisibleAndInvisibleWriteSelectedRawBytesToProcessStreams()
    {
        ExecutionResult result = CompileAndRunWithResult(
            FixtureSource("DotNet/1.4/FutureFeature/visible-and-invisible-write-selected-raw-bytes-to-process-streams/test.lol"));

        result.ExitCode.Should().Be(0);
        result.StandardOutputBytes.Should().Equal(0xC3);
        result.StandardErrorBytes.Should().Equal(0xA9);
    }

    [Fact]
    public void StdlibAndStringExposePinnedEdgeBehavior()
    {
        string output = CompileAndRun(
            FixtureSource("Shared/1.4/FutureFeature/stdlib-and-string-expose-pinned-edge-behavior/test.lol"));

        output.Replace("\r\n", "\n").TrimEnd('\n').Should().Be("0\n2\n[]\n[]");
    }

    [Fact]
    public void InvisibleMatchesVisibleArityAndNewlineRulesOnStandardError()
    {
        ExecutionResult result = CompileAndRunWithResult(
            FixtureSource("DotNet/1.4/FutureFeature/invisible-matches-visible-arity-and-newline-rules-on-standard-error/test.lol"));

        result.ExitCode.Should().Be(0);
        result.StandardOutput.Replace("\r\n", "\n").Should().Be("OUT\n");
        result.StandardError.Replace("\r\n", "\n").Should().Be("ERR 1 TWO\n");
    }

    [Fact]
    public void SystemCommandReturnsValidTextOutput()
    {
        File.WriteAllText(Path.Combine(TestDirectory, "valid-output.dat"), "HAI\n");
        string command = EscapeYarn(GetFileOutputCommand("valid-output.dat"));

        ExecutionResult result = CompileAndRunWithResult(
            FixtureSource("DotNet/1.4/FutureFeature/system-command-returns-valid-text-output/test.lol").Replace("{{command}}", command, StringComparison.Ordinal));

        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Be("HAI\n");
        result.StandardError.Should().BeEmpty();
    }

    [Fact]
    public void SystemCommandReturnsEmptyStandardOutput()
    {
        ExecutionResult result = CompileAndRunWithResult(
            FixtureSource("DotNet/1.4/FutureFeature/system-command-returns-empty-standard-output/test.lol"));

        result.ExitCode.Should().Be(0);
        result.StandardOutput.Replace("\r\n", "\n").Should().Be("0\n");
        result.StandardError.Should().BeEmpty();
    }

    [Fact]
    public void SystemCommandPreservesInvalidUtf8ThroughStringLenAndPrinting()
    {
        File.WriteAllBytes(Path.Combine(TestDirectory, "invalid-output.dat"), [0xC3]);
        string command = EscapeYarn(GetFileOutputCommand("invalid-output.dat"));

        ExecutionResult result = CompileAndRunWithResult(
            FixtureSource("DotNet/1.4/FutureFeature/system-command-preserves-invalid-utf8-through-string-len-and-printing/test.lol").Replace("{{command}}", command, StringComparison.Ordinal));

        result.ExitCode.Should().Be(0);
        result.StandardOutputBytes.Should().Equal(
            [.. Encoding.UTF8.GetBytes($"1{Environment.NewLine}"), 0xC3]);
        result.StandardErrorBytes.Should().BeEmpty();
    }

    [Fact]
    public void SystemCommandForwardsInvalidStandardErrorBytesUnchanged()
    {
        File.WriteAllBytes(Path.Combine(TestDirectory, "invalid-error.dat"), [0xC3]);
        string command = EscapeYarn(GetFileErrorCommand("invalid-error.dat"));

        ExecutionResult result = CompileAndRunWithResult(
            FixtureSource("DotNet/1.4/FutureFeature/system-command-forwards-invalid-standard-error-bytes-unchanged/test.lol").Replace("{{command}}", command, StringComparison.Ordinal));

        result.ExitCode.Should().Be(0);
        result.StandardOutputBytes.Should().BeEmpty();
        result.StandardErrorBytes.Should().Equal(0xC3);
    }

    private static string GetFileOutputCommand(string fileName) =>
        OperatingSystem.IsWindows()
            ? $"type {fileName}"
            : $"cat '{fileName}'";

    private static string GetFileErrorCommand(string fileName) =>
        OperatingSystem.IsWindows()
            ? $"type {fileName} 1>&2"
            : $"cat '{fileName}' >&2";

    private static string EscapeYarn(string value) =>
        value.Replace(":", "::", StringComparison.Ordinal)
            .Replace("\"", ":\"", StringComparison.Ordinal);
}
