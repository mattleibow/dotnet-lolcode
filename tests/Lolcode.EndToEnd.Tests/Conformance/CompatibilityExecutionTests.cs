using System.Diagnostics;
using System.Text;

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
            TimeSpan.FromSeconds(2));

        result.ExitCode.Should().Be(0);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Unix_supervisor_preserves_nonzero_exit_code_under_scheduling_pressure()
    {
        if (OperatingSystem.IsWindows() || !File.Exists("/bin/sh"))
            return;

        for (int attempt = 0; attempt < 20; attempt++)
        {
            var startInfo = new ProcessStartInfo("/bin/sh");
            startInfo.ArgumentList.Add("-c");
            startInfo.ArgumentList.Add("sleep 0.001 & exit 23");

            ProcessExecution result = await CompatibilityProcessRunner.RunAsync(
                startInfo,
                standardInput: null,
                TimeSpan.FromSeconds(2));

            result.ExitCode.Should().Be(23);
        }
    }

    [Fact]
    public async Task Unix_cleanup_is_quiet_and_allows_exact_output_cap()
    {
        if (OperatingSystem.IsWindows() || !File.Exists("/bin/sh"))
            return;

        var startInfo = new ProcessStartInfo("/bin/sh");
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("dd if=/dev/zero bs=1048576 count=4 2>/dev/null");

        ProcessExecution result = await CompatibilityProcessRunner.RunAsync(
            startInfo,
            standardInput: null,
            TimeSpan.FromSeconds(10));

        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().HaveCount(4 * 1024 * 1024);
        result.StandardError.Should().BeEmpty();
    }

    [Fact]
    public async Task Windows_job_terminates_descendant_after_successful_parent_exit()
    {
        if (!OperatingSystem.IsWindows())
            return;

        string command = """
            $child = Start-Process -FilePath $env:ComSpec -ArgumentList '/c', 'ping -n 30 127.0.0.1 > NUL' -PassThru
            [Console]::Out.WriteLine($child.Id)
            """;
        var startInfo = new ProcessStartInfo("powershell.exe");
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-Command");
        startInfo.ArgumentList.Add(command);

        ProcessExecution result = await CompatibilityProcessRunner.RunAsync(
            startInfo,
            standardInput: null,
            TimeSpan.FromSeconds(10));

        result.ExitCode.Should().Be(0);
        int processId = int.Parse(result.StandardOutputText.Trim(), System.Globalization.CultureInfo.InvariantCulture);
        await Task.Delay(100);
        Action findDescendant = () => Process.GetProcessById(processId);
        findDescendant.Should().Throw<ArgumentException>();
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

    [Fact]
    public void Runtime_error_fixture_rejects_a_compile_diagnostic_even_when_text_matches()
    {
        var test = new LciTestRegistration(
            "phase-regression",
            "phase-regression",
            Path.Combine(AppContext.BaseDirectory, "Compatibility", "KnownLciDivergence", "1.2", "Errors", "noob-in-arithmetic-throws-error", "test.lol"),
            null,
            null,
            Path.Combine(AppContext.BaseDirectory, "Compatibility", "KnownLciDivergence", "1.2", "Errors", "noob-in-arithmetic-throws-error", "test.err"),
            null,
            true,
            null);
        ProcessExecution compilationFailure = ProcessExecution.CompilationFailure([]);

        Action assertion = () => CompatibilityCorpusTests.AssertFixtureResult(
            "simulated", test, compilationFailure, validateDotNetPhase: true);

        assertion.Should().Throw<Exception>();
    }

    [Fact]
    public void Compile_diagnostic_fixture_rejects_a_runtime_failure_with_the_same_diagnostic_id()
    {
        var test = new LciTestRegistration(
            "phase-regression",
            "phase-regression",
            Path.Combine(AppContext.BaseDirectory, "Compatibility", "KnownLciDivergence", "1.2", "Loops", "undefined-custom-loop-operation-is-an-error", "test.lol"),
            null,
            null,
            null,
            Path.Combine(AppContext.BaseDirectory, "Compatibility", "KnownLciDivergence", "1.2", "Loops", "undefined-custom-loop-operation-is-an-error", "test.diag"),
            true,
            null);
        var runtimeFailure = new ProcessExecution(1, [], Encoding.UTF8.GetBytes("LOL2003"));

        Action assertion = () => CompatibilityCorpusTests.AssertFixtureResult(
            "simulated", test, runtimeFailure, validateDotNetPhase: true);

        assertion.Should().Throw<Exception>();
    }
}
