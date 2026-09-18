using System.Text;

namespace Lolcode.EndToEnd.Tests;

/// <summary>End-to-end coverage for pinned lci/future language behavior.</summary>
public class FutureFeatureTests : EndToEndTestBase
{
    [Fact]
    public void ImportsSupportOptionalQuestionSrsUnknownNamesAndBothCallSpellings()
    {
        AssertOutput(
            """
            HAI 1.4
            CAN HAS STRING'Z ignored
            CAN HAS UNKNOWN?
            I HAS A library ITZ "STRING"
            CAN HAS SRS library?
            VISIBLE I IZ STRING'Z LEN YR "HAI" MKAY
            VISIBLE STRING IZ AT YR "HAI" AN YR 1 MKAY
            KTHXBYE
            """,
            "3\nA");
    }

    [Fact]
    public void StdioSupportsEverySlotAndFailedOpen()
    {
        AssertOutput(
            """
            HAI 1.4
            CAN HAS STDIO?
            I HAS A file ITZ I IZ STDIO'Z OPEN YR "library.dat" AN YR "w+" MKAY
            I IZ STDIO'Z DIAF YR file MKAY
            O RLY?
              YA RLY
                VISIBLE "unexpected open failure"
              NO WAI
                VISIBLE "opened"
            OIC
            I IZ STDIO'Z SCRIBBEL YR file AN YR "HAI" MKAY
            I IZ STDIO'Z AGEIN YR file MKAY
            VISIBLE I IZ STDIO'Z LUK YR file AN YR 3 MKAY
            I IZ STDIO'Z CLOSE YR file MKAY
            I HAS A missing ITZ I IZ STDIO'Z OPEN YR "missing/path" AN YR "r" MKAY
            I IZ STDIO'Z DIAF YR missing MKAY
            O RLY?
              YA RLY
                VISIBLE "failed safely"
              NO WAI
                VISIBLE "unexpected success"
            OIC
            KTHXBYE
            """,
            "opened\nHAI\nfailed safely");
    }

    [Fact]
    public void StdioAppendAndErrorsMatchCStreamSemantics()
    {
        string path = Path.Combine(TestDirectory, "append.dat");
        File.WriteAllText(path, "A");

        AssertOutput(
            """
            HAI 1.4
            CAN HAS STDIO?
            I HAS A file ITZ I IZ STDIO'Z OPEN YR "append.dat" AN YR "a+" MKAY
            I IZ STDIO'Z AGEIN YR file MKAY
            I IZ STDIO'Z SCRIBBEL YR file AN YR "B" MKAY
            I IZ STDIO'Z AGEIN YR file MKAY
            VISIBLE I IZ STDIO'Z LUK YR file AN YR 2 MKAY
            I IZ STDIO'Z CLOSE YR file MKAY
            I HAS A readonly ITZ I IZ STDIO'Z OPEN YR "append.dat" AN YR "r" MKAY
            I IZ STDIO'Z SCRIBBEL YR readonly AN YR "NOPE" MKAY
            I IZ STDIO'Z DIAF YR readonly MKAY
            O RLY?
              YA RLY
                VISIBLE "failed safely"
              NO WAI
                VISIBLE "unexpected success"
            OIC
            KTHXBYE
            """,
            "AB\nfailed safely");

        File.ReadAllText(path).Should().Be("AB");
    }

    [Fact]
    public void StringAtPreservesBytesThroughAllYarnOperations()
    {
        ExecutionResult result = CompileAndRunWithResult(
            """
            HAI 1.4
            CAN HAS STRING?
            CAN HAS STDIO?
            I HAS A selected ITZ I IZ STRING'Z AT YR "é" AN YR 0 MKAY
            I HAS A selected2 ITZ I IZ STRING'Z AT YR "é" AN YR 1 MKAY
            I HAS A reassembled ITZ SMOOSH selected AN selected2 MKAY
            VISIBLE I IZ STRING'Z LEN YR reassembled MKAY
            BOTH SAEM reassembled AN "é"
            O RLY?
              YA RLY
                VISIBLE "same"
            OIC
            BOTH SAEM selected AN "Ã"
            O RLY?
              YA RLY
                VISIBLE "unexpected"
              NO WAI
                VISIBLE "different"
            OIC
            I HAS A explicitlyYarn ITZ MAEK selected A YARN
            BOTH SAEM explicitlyYarn AN selected
            O RLY?
              YA RLY
                VISIBLE "cast"
            OIC
            reassembled
            WTF?
              OMG "é"
                VISIBLE "switch"
                GTFO
              OMGWTF
                VISIBLE "unexpected switch"
            OIC
            VISIBLE reassembled
            VISIBLE ":{selected}:{selected2}"
            I HAS A file ITZ I IZ STDIO'Z OPEN YR "selected.dat" AN YR "w" MKAY
            I IZ STDIO'Z SCRIBBEL YR file AN YR selected MKAY
            I IZ STDIO'Z CLOSE YR file MKAY
            KTHXBYE
            """);

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
            """
            HAI 1.4
            CAN HAS STRING?
            I HAS A first ITZ I IZ STRING'Z AT YR "é" AN YR 0 MKAY
            I HAS A second ITZ I IZ STRING'Z AT YR "é" AN YR 1 MKAY
            VISIBLE first!
            INVISIBLE second!
            KTHXBYE
            """);

        result.ExitCode.Should().Be(0);
        result.StandardOutputBytes.Should().Equal(0xC3);
        result.StandardErrorBytes.Should().Equal(0xA9);
    }

    [Fact]
    public void StdlibAndStringExposePinnedEdgeBehavior()
    {
        string output = CompileAndRun(
            """
            HAI 1.4
            CAN HAS STDLIB?
            CAN HAS STRING?
            I IZ STDLIB'Z MIX YR 99 MKAY
            VISIBLE I IZ STDLIB'Z BLOW YR 0 MKAY
            VISIBLE I IZ STRING'Z LEN YR "é" MKAY
            VISIBLE "[" I IZ STRING'Z AT YR "HAI" AN YR -1 MKAY "]"
            VISIBLE "[" I IZ STRING'Z AT YR "HAI" AN YR 3 MKAY "]"
            KTHXBYE
            """);

        output.Replace("\r\n", "\n").TrimEnd('\n').Should().Be("0\n2\n[]\n[]");
    }

    [Fact]
    public void InvisibleMatchesVisibleArityAndNewlineRulesOnStandardError()
    {
        ExecutionResult result = CompileAndRunWithResult(
            """
            HAI 1.4
            INVISIBLE "ERR " AN 1!
            INVISIBLE " TWO"
            VISIBLE "OUT"
            KTHXBYE
            """);

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
            $$"""
            HAI 1.4
            VISIBLE I DUZ "{{command}}"!
            KTHXBYE
            """);

        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Be("HAI\n");
        result.StandardError.Should().BeEmpty();
    }

    [Fact]
    public void SystemCommandReturnsEmptyStandardOutput()
    {
        ExecutionResult result = CompileAndRunWithResult(
            """
            HAI 1.4
            CAN HAS STRING?
            I HAS A result ITZ I DUZ "cd ."
            VISIBLE I IZ STRING'Z LEN YR result MKAY
            KTHXBYE
            """);

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
            $$"""
            HAI 1.4
            CAN HAS STRING?
            I HAS A result ITZ I DUZ "{{command}}"
            VISIBLE I IZ STRING'Z LEN YR result MKAY
            VISIBLE result!
            KTHXBYE
            """);

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
            $$"""
            HAI 1.4
            I HAS A result ITZ I DUZ "{{command}}"
            VISIBLE result!
            KTHXBYE
            """);

        result.ExitCode.Should().Be(0);
        result.StandardOutputBytes.Should().BeEmpty();
        result.StandardErrorBytes.Should().Equal(0xC3);
    }

    [Fact]
    public void HasAnAndRNoobWorkForDirectSrsAndObjectSlots()
    {
        AssertOutput(
            """
            HAI 1.4
            I HAS AN direct ITZ 1
            I HAS A dynamicName ITZ "dynamic"
            I HAS SRS dynamicName ITZ 2
            I HAS A box ITZ A BUKKIT
            box HAS AN slot ITZ 3
            direct R NOOB
            SRS dynamicName R NOOB
            box'Z slot R NOOB
            BOTH SAEM direct AN NOOB
            O RLY?
              YA RLY
                VISIBLE "direct"
            OIC
            BOTH SAEM SRS dynamicName AN NOOB
            O RLY?
              YA RLY
                VISIBLE "srs"
            OIC
            BOTH SAEM box'Z slot AN NOOB
            O RLY?
              YA RLY
                VISIBLE "slot"
            OIC
            KTHXBYE
            """,
            "direct\nsrs\nslot");
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
