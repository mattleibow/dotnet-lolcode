using Lolcode.CodeAnalysis;
using Lolcode.CodeAnalysis.Scripting;
using Lolcode.Web.Execution;

namespace Lolcode.Web.Tests;

public sealed class LolcodeCodeRunnerTests
{
    private const string HelloProgram = """
        HAI 1.2
          VISIBLE "HAI"
        KTHXBYE
        """;

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
    public void TruncateOutput_PreservesBoundaryAndMarksOverflow()
    {
        var boundary = new string('X', CodeRunnerLimits.MaxOutputLength);
        var overflow = string.Concat(boundary, "Y");

        LolcodeCodeRunner.TruncateOutput(boundary).Should().Be(boundary);
        LolcodeCodeRunner.TruncateOutput(overflow).Should().Be(
            string.Concat(boundary, Environment.NewLine, "[output truncated]"));
        LolcodeCodeRunner.TruncateOutput("short", isTruncated: true).Should().Be(
            string.Concat("short", Environment.NewLine, "[output truncated]"));
    }

    [Fact]
    public async Task RunAsync_BoundsCapturedOutputDuringExecution()
    {
        var result = await _runner.RunAsync(
            new CodeRunRequest(
                """
                HAI 1.2
                  IM IN YR loop UPPIN YR i TIL BOTH SAEM i AN 128001
                    VISIBLE "X"!
                  IM OUTTA YR loop
                KTHXBYE
                """,
                string.Empty));

        result.Success.Should().BeTrue();
        result.Output.Should().Be(
            string.Concat(
                new string('X', CodeRunnerLimits.MaxOutputLength),
                Environment.NewLine,
                "[output truncated]"));
    }

    [Fact]
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
            new CodeRunRequest(RuntimeErrorProgram, string.Empty));

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
        var script = CreateScript(RuntimeErrorProgram);
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

    private const string RuntimeErrorProgram = """
        HAI 1.2
          I HAS A value
          VISIBLE SUM OF value AN 1
        KTHXBYE
        """;
}
