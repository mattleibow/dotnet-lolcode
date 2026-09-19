using System.Reflection;
using System.Runtime.Loader;
using Lolcode.CodeAnalysis.Syntax;
using Lolcode.CodeAnalysis.Text;
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

    [Theory]
    [InlineData("1.3")]
    [InlineData("1.4")]
    public void CrossFileRuntimeFunctionCalls_AreHoistedBeforeTopLevelExecution(string version)
    {
        var caller = Tree(
            $$"""
            HAI {{version}}
            VISIBLE I IZ GREETING MKAY
            KTHXBYE
            """,
            "Caller.lol");
        var callee = Tree(
            $$"""
            HAI {{version}}
            HOW IZ I GREETING
                FOUND YR "HAI FROM ANOTHER FILE"
            IF U SAY SO
            KTHXBYE
            """,
            "Callee.lol");

        Execute(LolcodeCompilation.Create(callee, caller))
            .Should().Be($"HAI FROM ANOTHER FILE{Environment.NewLine}");
        Execute(LolcodeCompilation.Create(caller, callee))
            .Should().Be($"HAI FROM ANOTHER FILE{Environment.NewLine}");
    }

    [Theory]
    [InlineData("1.3")]
    [InlineData("1.4")]
    public void CrossFileRuntimeFunctionValues_AreHoistedBeforeTheyCanBeReferenced(string version)
    {
        var declaration = Tree(
            $$"""
            HAI {{version}}
            HOW IZ I GREETING
                FOUND YR "INITIALIZED"
            IF U SAY SO
            KTHXBYE
            """,
            "Declaration.lol");
        var initializationAndCall = Tree(
            $$"""
            HAI {{version}}
            I HAS A replacement ITZ GREETING
            VISIBLE I IZ replacement MKAY
            KTHXBYE
            """,
            "Initialization.lol");

        Execute(LolcodeCompilation.Create(declaration, initializationAndCall))
            .Should().Be($"INITIALIZED{Environment.NewLine}");
        Execute(LolcodeCompilation.Create(initializationAndCall, declaration))
            .Should().Be($"INITIALIZED{Environment.NewLine}");
    }

    [Theory]
    [InlineData("1.3")]
    [InlineData("1.4")]
    public void SingleFileRuntimeFunctionCalls_KeepTextualDeclarationOrder(string version)
    {
        var compilation = LolcodeCompilation.Create(Tree(
            $$"""
            HAI {{version}}
            VISIBLE I IZ GREETING MKAY
            HOW IZ I GREETING
                FOUND YR "TOO LATE"
            IF U SAY SO
            KTHXBYE
            """,
            "SingleFile.lol"));

        Action callBeforeDeclaration = () => Execute(compilation);
        callBeforeDeclaration.Should().Throw<TargetInvocationException>()
            .Which.InnerException.Should().BeOfType<LolRuntimeException>();
    }

    [Theory]
    [InlineData("1.3")]
    [InlineData("1.4")]
    public void CrossFileRuntimeFunctionReplacement_IsNotResetAtOriginalDeclarationPosition(string version)
    {
        var replacement = Tree(
            $$"""
            HAI {{version}}
            HOW IZ I REPLACEMENT YR value
                FOUND YR SMOOSH "REPLACED " AN value MKAY
            IF U SAY SO
            GREETING R REPLACEMENT
            VISIBLE I IZ GREETING YR "VALUE" MKAY
            KTHXBYE
            """,
            "Replacement.lol");
        var declaration = Tree(
            $$"""
            HAI {{version}}
            HOW IZ I GREETING
                FOUND YR "ORIGINAL"
            IF U SAY SO
            KTHXBYE
            """,
            "Declaration.lol");

        Execute(LolcodeCompilation.Create(replacement, declaration))
            .Should().Be($"REPLACED VALUE{Environment.NewLine}");
    }

    [Theory]
    [InlineData("1.3")]
    [InlineData("1.4")]
    public void CrossFileRuntimeFunctions_SupportMutualRecursion(string version)
    {
        var first = Tree(
            $$"""
            HAI {{version}}
            HOW IZ I FIRST YR value
                FOUND YR I IZ SECOND YR value MKAY
            IF U SAY SO
            VISIBLE I IZ FIRST YR 1 MKAY
            KTHXBYE
            """,
            "First.lol");
        var second = Tree(
            $$"""
            HAI {{version}}
            HOW IZ I SECOND YR value
                BOTH SAEM value AN 0
                O RLY?
                    YA RLY
                        FOUND YR "DONE"
                    NO WAI
                        FOUND YR I IZ FIRST YR DIFF OF value AN 1 MKAY
                OIC
            IF U SAY SO
            KTHXBYE
            """,
            "Second.lol");

        Execute(LolcodeCompilation.Create(first, second))
            .Should().Be($"DONE{Environment.NewLine}");
    }

    [Theory]
    [InlineData("1.3")]
    [InlineData("1.4")]
    public void CrossFileDynamicFunctionDeclarations_KeepTextualOrder(string version)
    {
        var caller = Tree(
            $$"""
            HAI {{version}}
            I HAS A functionName ITZ "GREETING"
            VISIBLE I IZ SRS functionName MKAY
            KTHXBYE
            """,
            "Caller.lol");
        var declaration = Tree(
            $$"""
            HAI {{version}}
            HOW IZ I SRS functionName
                FOUND YR "TOO LATE"
            IF U SAY SO
            KTHXBYE
            """,
            "DynamicDeclaration.lol");

        Action callBeforeDynamicDeclaration = () =>
            Execute(LolcodeCompilation.Create(caller, declaration));
        callBeforeDynamicDeclaration.Should().Throw<TargetInvocationException>()
            .Which.InnerException.Should().BeOfType<LolRuntimeException>();
    }

    [Theory]
    [InlineData("1.3")]
    [InlineData("1.4")]
    public void CrossFileObjectMethods_KeepTextualOrderAndObjectScope(string version)
    {
        var caller = Tree(
            $$"""
            HAI {{version}}
            VISIBLE I IZ box'Z GREETING MKAY
            KTHXBYE
            """,
            "Caller.lol");
        var objectDeclaration = Tree(
            $$"""
            HAI {{version}}
            O HAI IM box
                HOW IZ I GREETING
                    FOUND YR "TOO LATE"
                IF U SAY SO
            KTHX
            KTHXBYE
            """,
            "Object.lol");

        Action callBeforeObjectInitialization = () =>
            Execute(LolcodeCompilation.Create(caller, objectDeclaration));
        callBeforeObjectInitialization.Should().Throw<TargetInvocationException>()
            .Which.InnerException.Should().BeOfType<LolRuntimeException>();
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
    public void SourceTextConstruction_PreservesEffectiveFileNamesInSyntaxAndBinderDiagnostics()
    {
        string loadPath = Path.Combine(
            AppContext.BaseDirectory,
            $"source-text-{Guid.NewGuid():N}.lol");
        File.WriteAllText(loadPath, "HAI 1.2\nVISIBLE missing\nKTHXBYE");

        try
        {
            var syntaxTree = SyntaxTree.ParseText(
                SourceText.From("HAI 1.2\nVISIBLE\nKTHXBYE", "Ignored.lol"),
                "SyntaxSourceText.lol");
            syntaxTree.FilePath.Should().Be("SyntaxSourceText.lol");
            syntaxTree.Text.FileName.Should().Be("SyntaxSourceText.lol");
            syntaxTree.Diagnostics.Should().ContainSingle(d => d.Id == "LOL1006")
                .Which.Location.FileName.Should().Be("SyntaxSourceText.lol");

            var compilation = LolcodeCompilation.Create(
                SyntaxTree.ParseText("HAI 1.2\nVISIBLE missing\nKTHXBYE", "String.lol"),
                SyntaxTree.ParseText(SourceText.From(
                    "HAI 1.2\nVISIBLE missing\nKTHXBYE",
                    "SourceText.lol")),
                SyntaxTree.Load(loadPath));

            compilation.GetDiagnostics()
                .Where(d => d.Id == "LOL2001")
                .Select(d => d.Location.FileName)
                .Should()
                .BeEquivalentTo(["String.lol", "SourceText.lol", loadPath]);
        }
        finally
        {
            File.Delete(loadPath);
        }
    }

    [Fact]
    public void SourceTextConstruction_PreservesEffectiveFileNamesInVersionDiagnostics()
    {
        string loadPath = Path.Combine(
            AppContext.BaseDirectory,
            $"version-source-text-{Guid.NewGuid():N}.lol");
        File.WriteAllText(loadPath, "HAI 1.3\nKTHXBYE");

        try
        {
            var diagnostics = LolcodeCompilation.Create(
                    SyntaxTree.ParseText("HAI 1.2\nKTHXBYE", "String.lol"),
                    SyntaxTree.ParseText(SourceText.From(
                        "HAI 1.4\nKTHXBYE",
                        "SourceText.lol")),
                    SyntaxTree.Load(loadPath))
                .GetDiagnostics()
                .Where(d => d.Id == "LOL2011")
                .ToArray();

            diagnostics.Select(d => d.Location.FileName)
                .Should().BeEquivalentTo(["SourceText.lol", loadPath]);
        }
        finally
        {
            File.Delete(loadPath);
        }
    }

    [Fact]
    public void DuplicateFunctions_KeepTheirOwnParametersWhileCallsUseTheWinner()
    {
        const string source = """
            HAI 1.2
            HOW IZ I SAME YR winner
                FOUND YR winner
            IF U SAY SO
            HOW IZ I SAME
                FOUND YR 0
            IF U SAY SO
            VISIBLE I IZ SAME MKAY
            KTHXBYE
            """;

        var diagnostics = LolcodeCompilation.Create(Tree(source, "DuplicateFunction.lol"))
            .GetDiagnostics();

        diagnostics.Should().ContainSingle(d => d.Id == "LOL2010")
            .Which.Location.FileName.Should().Be("DuplicateFunction.lol");
        diagnostics.Should().ContainSingle(d => d.Id == "LOL2004")
            .Which.Message.Should().Contain("expects 1 argument(s) but got 0");
    }

    [Fact]
    public void CrossFileDuplicateFunctions_KeepWinnerArityAndLocations()
    {
        var diagnostics = LolcodeCompilation.Create(
                Tree(
                    "HAI 1.2\nHOW IZ I SAME YR winner\nFOUND YR winner\nIF U SAY SO\nKTHXBYE",
                    "Winner.lol"),
                Tree(
                    "HAI 1.2\nHOW IZ I SAME\nFOUND YR 0\nIF U SAY SO\nVISIBLE I IZ SAME MKAY\nKTHXBYE",
                    "Duplicate.lol"))
            .GetDiagnostics();

        diagnostics.Should().ContainSingle(d => d.Id == "LOL2010")
            .Which.Location.FileName.Should().Be("Duplicate.lol");
        diagnostics.Should().ContainSingle(d => d.Id == "LOL2004")
            .Which.Location.FileName.Should().Be("Duplicate.lol");
    }

    [Fact]
    public void DuplicateFunctionParameters_ReportTheDuplicateParameterWithoutThrowing()
    {
        var diagnostics = LolcodeCompilation.Create(Tree(
                """
                HAI 1.2
                HOW IZ I SAME YR repeated AN YR repeated
                    FOUND YR repeated
                IF U SAY SO
                VISIBLE I IZ SAME YR 1 MKAY
                KTHXBYE
                """,
                "DuplicateParameter.lol"))
            .GetDiagnostics();

        diagnostics.Should().ContainSingle(d => d.Id == "LOL2002")
            .Which.Location.FileName.Should().Be("DuplicateParameter.lol");
        diagnostics.Should().ContainSingle(d => d.Id == "LOL2004")
            .Which.Message.Should().Contain("expects 2 argument(s) but got 1");
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
