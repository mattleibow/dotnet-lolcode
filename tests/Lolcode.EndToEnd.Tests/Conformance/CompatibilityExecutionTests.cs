using System.Diagnostics;

namespace Lolcode.EndToEnd.Tests;

/// <summary>Verifies bounded process execution and byte-preserving fixture comparison.</summary>
public sealed class CompatibilityExecutionTests
{
    [Fact]
    public async Task Timeout_includes_pipe_drains_after_parent_exits()
    {
        if (OperatingSystem.IsWindows() || !File.Exists("/bin/sh"))
            return;

        var startInfo = new ProcessStartInfo("/bin/sh");
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("sleep 0.1; sleep 30 &");
        var stopwatch = Stopwatch.StartNew();

        Func<Task> run = () => CompatibilityProcessRunner.RunAsync(
            startInfo,
            standardInput: null,
            TimeSpan.FromMilliseconds(500));

        await run.Should().ThrowAsync<TimeoutException>();
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
