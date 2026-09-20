using System.Diagnostics;

namespace Lolcode.EndToEnd.Tests;

/// <summary>Verifies bounded process execution and byte-preserving fixture comparison.</summary>
public sealed class CompatibilityExecutionTests
{
    [Fact]
    public async Task Parent_exit_with_descendant_pipes_does_not_hang_drains()
    {
        if (OperatingSystem.IsWindows() || !File.Exists("/bin/sh"))
            return;

        var startInfo = new ProcessStartInfo("/bin/sh");
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("sleep 0.1; sleep 30 &");
        var stopwatch = Stopwatch.StartNew();

        ProcessExecution result = await CompatibilityProcessRunner.RunAsync(
            startInfo,
            standardInput: null,
            TimeSpan.FromMilliseconds(500));

        result.ExitCode.Should().Be(0);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Byte_normalization_only_replaces_CRLF()
    {
        byte[] original = [0x80, 0x0D, 0x0A, 0x81, 0xEF, 0xBF, 0xBD, 0x0D];

        byte[] normalized = CompatibilityCorpusTests.NormalizeLineEndings(original);
        normalized
            .Should()
            .Equal([0x80, 0x0A, 0x81, 0xEF, 0xBF, 0xBD, 0x0D]);
        normalized[0].Should().Be(0x80);
        normalized[2].Should().Be(0x81);
        normalized[3..6].Should().Equal([0xEF, 0xBF, 0xBD]);
    }
}
