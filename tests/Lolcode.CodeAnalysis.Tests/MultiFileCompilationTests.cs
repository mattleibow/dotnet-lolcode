using System.Reflection;
using System.Runtime.Loader;
using Lolcode.CodeAnalysis.Syntax;
using Lolcode.Runtime;

namespace Lolcode.CodeAnalysis.Tests;

/// <summary>Behavioral tests for compilation-wide multi-file binding and emission.</summary>
public sealed class MultiFileCompilationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CrossFileFunctionCalls_AreIndependentOfSourceFileOrder(bool calleeFirst)
    {
        var caller = Tree(
            """
            HAI 1.2
            VISIBLE I IZ GREETING MKAY
            KTHXBYE
            """,
            "Caller.lol");
        var callee = Tree(
            """
            HAI 1.2
            HOW IZ I GREETING
                FOUND YR "HAI FROM ANOTHER FILE"
            IF U SAY SO
            KTHXBYE
            """,
            "Callee.lol");

        var compilation = calleeFirst
            ? LolcodeCompilation.Create(callee, caller)
            : LolcodeCompilation.Create(caller, callee);

        compilation.GetDiagnostics().Should().NotContain(d => d.Severity == DiagnosticSeverity.Error);
        Execute(compilation).Should().Be($"HAI FROM ANOTHER FILE{Environment.NewLine}");
    }

    [Fact]
    public void CrossFileTopLevelStatements_ExecuteInSyntaxTreeOrder()
    {
        var compilation = LolcodeCompilation.Create(
            Tree("HAI 1.2\nVISIBLE \"FIRST\"\nKTHXBYE", "First.lol"),
            Tree("HAI 1.2\nVISIBLE \"SECOND\"\nKTHXBYE", "Second.lol"));

        Execute(compilation).Should().Be(
            $"FIRST{Environment.NewLine}SECOND{Environment.NewLine}");
    }

    [Fact]
    public void CrossFileVariables_RespectSyntaxTreeOrder()
    {
        var declaringTree = Tree(
            "HAI 1.2\nI HAS A value ITZ 42\nKTHXBYE",
            "Declaration.lol");
        var usingTree = Tree(
            "HAI 1.2\nVISIBLE value\nKTHXBYE",
            "Use.lol");

        var inOrder = LolcodeCompilation.Create(declaringTree, usingTree);
        Execute(inOrder).Should().Be($"42{Environment.NewLine}");

        var outOfOrder = LolcodeCompilation.Create(
            Tree("HAI 1.2\nVISIBLE value\nKTHXBYE", "UseFirst.lol"),
            Tree("HAI 1.2\nI HAS A value ITZ 42\nKTHXBYE", "DeclarationSecond.lol"));
        outOfOrder.GetDiagnostics().Should().ContainSingle(d => d.Id == "LOL2001")
            .Which.Location.FileName.Should().Be("UseFirst.lol");
    }

    [Fact]
    public void CrossFileDuplicateDeclarations_ReportTheDuplicateFile()
    {
        var duplicateFunction = LolcodeCompilation.Create(
            Tree("HAI 1.2\nHOW IZ I SAME\nFOUND YR 1\nIF U SAY SO\nKTHXBYE", "First.lol"),
            Tree("HAI 1.2\nHOW IZ I SAME\nFOUND YR 2\nIF U SAY SO\nKTHXBYE", "DuplicateFunction.lol"));
        duplicateFunction.GetDiagnostics().Should().ContainSingle(d => d.Id == "LOL2010")
            .Which.Location.FileName.Should().Be("DuplicateFunction.lol");

        var duplicateVariable = LolcodeCompilation.Create(
            Tree("HAI 1.2\nI HAS A value\nKTHXBYE", "First.lol"),
            Tree("HAI 1.2\nI HAS A value\nKTHXBYE", "DuplicateVariable.lol"));
        duplicateVariable.GetDiagnostics().Should().ContainSingle(d => d.Id == "LOL2002")
            .Which.Location.FileName.Should().Be("DuplicateVariable.lol");
    }

    [Fact]
    public void CrossFileVersions_MustMatch()
    {
        var matching = LolcodeCompilation.Create(
            Tree("HAI 1.4\nKTHXBYE", "One.lol"),
            Tree("HAI 1.4\nKTHXBYE", "Two.lol"));
        matching.GetDiagnostics().Should().NotContain(d => d.Id == "LOL2011");

        var mismatched = LolcodeCompilation.Create(
            Tree("HAI 1.2\nKTHXBYE", "One.lol"),
            Tree("HAI 1.4\nKTHXBYE", "Two.lol"),
            Tree("HAI 1.3\nKTHXBYE", "Three.lol"));
        mismatched.GetDiagnostics().Where(d => d.Id == "LOL2011")
            .Select(d => d.Location.FileName)
            .Should().BeEquivalentTo(["Two.lol", "Three.lol"]);
    }

    [Fact]
    public void CrossFileSyntaxDiagnostics_AreAggregated()
    {
        var diagnostics = LolcodeCompilation.Create(
                Tree("HAI 1.2\nVISIBLE\nKTHXBYE", "BrokenOne.lol"),
                Tree("HAI 1.2\nVISIBLE\nKTHXBYE", "BrokenTwo.lol"))
            .GetDiagnostics()
            .Where(d => d.Id == "LOL1006")
            .ToArray();

        diagnostics.Select(d => d.Location.FileName)
            .Should().BeEquivalentTo(["BrokenOne.lol", "BrokenTwo.lol"]);
    }

    [Fact]
    public void ZeroTreeCompilation_EmitsAnEmptyExecutable()
    {
        var compilation = LolcodeCompilation.Create();
        compilation.GetDiagnostics().Should().BeEmpty();
        using var peStream = new MemoryStream();

        var result = compilation.Emit(peStream);

        result.Success.Should().BeTrue();
        peStream.Length.Should().BeGreaterThan(0);
    }

    private static SyntaxTree Tree(string source, string filePath) =>
        SyntaxTree.ParseText(source, filePath);

    private static string Execute(LolcodeCompilation compilation)
    {
        using var peStream = new MemoryStream();
        var result = compilation.Emit(peStream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        peStream.Position = 0;

        var loadContext = new AssemblyLoadContext($"MultiFile_{Guid.NewGuid():N}", isCollectible: true);
        loadContext.Resolving += (_, assemblyName) =>
            AssemblyName.ReferenceMatchesDefinition(
                assemblyName,
                typeof(LolRuntime).Assembly.GetName())
                ? typeof(LolRuntime).Assembly
                : null;
        try
        {
            var assembly = loadContext.LoadFromStream(peStream);
            using var output = new StringWriter();
            using var ioScope = LolRuntime.PushIo(new StringReader(string.Empty), output, TextWriter.Null);
            assembly.EntryPoint!.Invoke(null, null);
            return output.ToString();
        }
        finally
        {
            loadContext.Unload();
        }
    }
}
