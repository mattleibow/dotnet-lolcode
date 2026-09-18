using Lolcode.CodeAnalysis.Syntax;

namespace Lolcode.CodeAnalysis.Tests;

/// <summary>
/// Tests semantic binding rules that require a complete compilation.
/// </summary>
public class BindingTests
{
    [Fact]
    public void BindSwitch_AllowsEqualValuesOfDifferentLiteralTypes()
    {
        var diagnostics = GetDiagnostics("""
            HAI 1.3
            1
            WTF?
              OMG WIN
              OMG 1.0
              OMG "1"
              OMG 1
            OIC
            KTHXBYE
            """);

        diagnostics.Should().NotContain(diagnostic => diagnostic.Id == "LOL2008");
    }

    [Fact]
    public void BindSwitch_RejectsDuplicateLiteralWithSameTypeAndValue()
    {
        var diagnostics = GetDiagnostics("""
            HAI 1.3
            1
            WTF?
              OMG 1
              OMG 1
            OIC
            KTHXBYE
            """);

        diagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == "LOL2008");
    }

    [Fact]
    public void BindSwitch_RejectsDuplicateYarnValuesAfterEscapeResolution()
    {
        var diagnostics = GetDiagnostics("""
            HAI 1.3
            "A"
            WTF?
              OMG "A"
              OMG ":(41)"
            OIC
            KTHXBYE
            """);

        diagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == "LOL2008");
    }

    private static IReadOnlyList<Diagnostic> GetDiagnostics(string source)
    {
        var syntaxTree = SyntaxTree.ParseText(source);
        return LolcodeCompilation.Create(syntaxTree).GetDiagnostics();
    }
}
