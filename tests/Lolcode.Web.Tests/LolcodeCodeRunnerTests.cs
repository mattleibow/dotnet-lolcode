using Lolcode.CodeAnalysis;
using Lolcode.CodeAnalysis.Scripting;
using Lolcode.Web.Execution;

namespace Lolcode.Web.Tests;

public sealed class LolcodeCodeRunnerTests
{
    private static string HelloProgram => File.ReadAllText(Path.Combine(
        AppContext.BaseDirectory, "Compatibility", "DotNet", "1.2", "Web", "code-runner-hello", "test.lol"));

    private readonly LolcodeCodeRunner _runner = new();

    [Fact]
    public async Task RunAsync_RejectsSourceOverLimit()
    {
        var result = await _runner.RunAsync(
            new CodeRunRequest(
                new string('X', CodeRunnerLimits.MaxSourceLength + 1),
                string.Empty));

        result.Success.Should().BeFalse();
        result.Executed.Should().BeFalse();
        result.Diagnostics.Should().ContainSingle()
            .Which.Id.Should().Be("INPUT");
    }

    [Fact]
    public async Task RunAsync_RejectsInputOverLimit()
    {
        var result = await _runner.RunAsync(
            new CodeRunRequest(
                HelloProgram,
                new string('X', CodeRunnerLimits.MaxInputLength + 1)));

        result.Success.Should().BeFalse();
        result.Executed.Should().BeFalse();
        result.Diagnostics.Should().ContainSingle()
            .Which.Id.Should().Be("INPUT");
    }

    [Fact]
    public async Task RunAsync_ReusesScriptsAndRequiresReloadAfterCacheLimit()
    {
        var firstResult = await _runner.RunAsync(new CodeRunRequest(HelloProgram, string.Empty));
        firstResult.Success.Should().BeTrue();

        for (var index = 1; index < CodeRunnerLimits.MaxCachedScripts; index++)
        {
            var result = await _runner.RunAsync(
                new CodeRunRequest(
                    string.Concat(HelloProgram, Environment.NewLine, $"BTW {index}"),
                    string.Empty));

            result.Success.Should().BeTrue();
        }

        _runner.CachedScriptCount.Should().Be(CodeRunnerLimits.MaxCachedScripts);

        var cachedResult = await _runner.RunAsync(new CodeRunRequest(HelloProgram, string.Empty));
        cachedResult.Success.Should().BeTrue();
        _runner.CachedScriptCount.Should().Be(CodeRunnerLimits.MaxCachedScripts);

        var limitResult = await _runner.RunAsync(
            new CodeRunRequest(string.Concat(HelloProgram, Environment.NewLine, "BTW next"), string.Empty));
        limitResult.Executed.Should().BeFalse();
        limitResult.Diagnostics.Should().ContainSingle()
            .Which.Message.Should().Contain("Reload the page");
    }

    [Fact]
    public async Task RunAsync_UsesCurrentRuntimeAssembliesWithoutRuntimePathDiagnostics()
    {
        var result = await _runner.RunAsync(new CodeRunRequest(HelloProgram, string.Empty));

        result.Success.Should().BeTrue();
        result.Executed.Should().BeTrue();
        result.StandardOutput.Should().Be("HAI" + Environment.NewLine);
        result.Diagnostics.Should().NotContain(diagnostic =>
            diagnostic.Message.Contains("System.Runtime", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AppendTruncationMarker_MarksOnlyTruncatedStreams()
    {
        LolcodeCodeRunner.AppendTruncationMarker(
                "complete",
                isTruncated: false,
                "[standard output truncated]")
            .Should().Be("complete");
        LolcodeCodeRunner.AppendTruncationMarker(
                "partial",
                isTruncated: true,
                "[standard output truncated]")
            .Should().Be(
                string.Concat("partial", Environment.NewLine, "[standard output truncated]"));
        LolcodeCodeRunner.AppendTruncationMarker(
                string.Empty,
                isTruncated: true,
                "[standard error truncated]")
            .Should().Be("[standard error truncated]");
    }

    [Theory]
    [InlineData(CodeRunnerLimits.MaxStandardStreamBytes, false)]
    [InlineData(CodeRunnerLimits.MaxStandardStreamBytes + 1, true)]
    [InlineLolcodeProgramException("This test constructs source text to exercise the targeted compiler behavior.")]
    public async Task RunAsync_BoundsCapturedOutputDuringExecution(
        int outputLength,
        bool shouldBeTruncated)
    {
        var result = await _runner.RunAsync(
            new CodeRunRequest(
                $$"""
                HAI 1.2
                  IM IN YR loop UPPIN YR i TIL BOTH SAEM i AN {{outputLength}}
                    VISIBLE "X"!
                  IM OUTTA YR loop
                KTHXBYE
                """,
                string.Empty));

        result.Success.Should().BeTrue();
        var expectedOutput = new string('X', CodeRunnerLimits.MaxStandardStreamBytes);
        if (shouldBeTruncated)
        {
            expectedOutput = string.Concat(
                expectedOutput,
                Environment.NewLine,
                "[standard output truncated]");
        }

        result.StandardOutput.Should().Be(expectedOutput);
        result.StandardError.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsync_CapturesAndBoundsStandardErrorIndependently()
    {
        var result = await _runner.RunAsync(
            new CodeRunRequest(
                File.ReadAllText(Path.Combine(
                    AppContext.BaseDirectory,
                    "Compatibility",
                    "DotNet",
                    "1.2",
                    "Web",
                    "code-runner-standard-error",
                    "test.lol")),
                string.Empty));

        result.Success.Should().BeTrue();
        result.StandardOutput.Should().Be("stdout");
        result.StandardError.Should().Be(
            string.Concat(
                new string('E', CodeRunnerLimits.MaxStandardStreamBytes),
                Environment.NewLine,
                "[standard error truncated]"));
    }

    [Fact]
    [InlineLolcodeProgramException("This test constructs source text to exercise the targeted compiler behavior.")]
    public async Task RunAsync_MapsCompilerDiagnosticLocation()
    {
        var result = await _runner.RunAsync(
            new CodeRunRequest(
                """
                HAI 1.2
                  VISIBLE missing
                KTHXBYE
                """,
                string.Empty));

        result.Diagnostics.Should().ContainSingle();
        var diagnostic = result.Diagnostics[0];
        diagnostic.Id.Should().Be("LOL2001");
        diagnostic.StartLine.Should().Be(2);
        diagnostic.StartColumn.Should().Be(11);
    }

    [Fact]
    public async Task RunAsync_MapsRuntimeDiagnosticLocation()
    {
        var result = await _runner.RunAsync(
            new CodeRunRequest(GetRuntimeErrorProgram(), string.Empty));

        result.Success.Should().BeFalse();
        result.Executed.Should().BeTrue();
        result.Diagnostics.Should().ContainSingle();
        var diagnostic = result.Diagnostics[0];
        diagnostic.Id.Should().Be("RUNTIME");
        diagnostic.StartLine.Should().Be(3);
        diagnostic.StartColumn.Should().Be(3);
    }

    [Fact]
    public void FindPortablePdbLocation_MapsRuntimeFrame()
    {
        var script = CreateScript(GetRuntimeErrorProgram());
        var state = script.Run();
        var compilation = state.Script.GetCompilation();

        state.Exception.Should().NotBeNull();
        LolcodeCodeRunner.FindPortablePdbLocation(
                state.Exception!,
                compilation)
            .Should().Be((3, 3));
    }

    [Fact]
    public void CreateRuntimeDiagnostic_OmitsUnavailableLocation()
    {
        var diagnostic = LolcodeCodeRunner.CreateRuntimeDiagnostic(
            new InvalidOperationException("boom"),
            CreateCompilation(HelloProgram));

        diagnostic.Id.Should().Be("RUNTIME");
        diagnostic.StartLine.Should().BeNull();
        diagnostic.StartColumn.Should().BeNull();
    }

    private static LolcodeCompilation CreateCompilation(string source) =>
        CreateScript(source).GetCompilation();

    private static LolcodeScript CreateScript(string source) =>
        LolcodeScript.Create(source, new LolcodeScriptOptions
        {
            FilePath = "Program.lol",
        });

    [InlineLolcodeProgramException("The runtime diagnostic test requires a precise failing source location.")]
    private static string GetRuntimeErrorProgram()
    {
        return """
            HAI 1.2
              I HAS A value
              VISIBLE SUM OF value AN 1
            KTHXBYE
            """;
    }
}
