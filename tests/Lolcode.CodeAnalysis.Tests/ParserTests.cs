using Lolcode.CodeAnalysis.Syntax;
using Lolcode.CodeAnalysis.Text;

namespace Lolcode.CodeAnalysis.Tests;

/// <summary>
/// Tests for the LOLCODE parser.
/// </summary>
public class ParserTests
{
    private static CompilationUnitSyntax Parse(string input)
    {
        var tree = SyntaxTree.ParseText(input);
        return tree.Root;
    }

    private static CompilationUnitSyntax ParseWithDiagnostics(string input, out IReadOnlyList<Diagnostic> diagnostics)
    {
        var tree = SyntaxTree.ParseText(input);
        diagnostics = tree.Diagnostics.ToList();
        return tree.Root;
    }

    [Fact]
    public void Parse_MinimalProgram()
    {
        var tree = Parse("HAI 1.2\nKTHXBYE");
        tree.Program.Should().NotBeNull();
        tree.Program.HaiKeyword.Kind.Should().Be(SyntaxKind.HaiKeyword);
        tree.Program.KthxbyeKeyword.Kind.Should().Be(SyntaxKind.KthxbyeKeyword);
    }

    [Fact]
    public void Parse_HaiWithoutVersion_ReportsDiagnostic()
    {
        ParseWithDiagnostics("HAI\nKTHXBYE", out var diagnostics);

        diagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == "LOL1005");
    }

    [Fact]
    public void Parse_VariableDeclaration_NoInit()
    {
        var tree = Parse("HAI 1.2\nI HAS A x\nKTHXBYE");
        tree.Program.Statements.Should().ContainSingle()
            .Which.Should().BeOfType<VariableDeclarationSyntax>()
            .Which.NameToken.Text.Should().Be("x");
    }

    [Fact]
    public void Parse_VariableDeclaration_WithInit()
    {
        var tree = Parse("HAI 1.2\nI HAS A x ITZ 42\nKTHXBYE");
        var decl = tree.Program.Statements.Should().ContainSingle()
            .Which.Should().BeOfType<VariableDeclarationSyntax>().Subject;
        decl.NameToken.Text.Should().Be("x");
        decl.Initializer.Should().NotBeNull();
    }

    [Fact]
    public void Parse_Assignment()
    {
        var tree = Parse("HAI 1.2\nI HAS A x\nx R 10\nKTHXBYE");
        tree.Program.Statements.Should().HaveCount(2);
        tree.Program.Statements[1].Should().BeOfType<AssignmentStatementSyntax>();
    }

    [Fact]
    public void Parse_Visible_SingleArg()
    {
        var tree = Parse("HAI 1.2\nVISIBLE \"hello\"\nKTHXBYE");
        var visible = tree.Program.Statements.Should().ContainSingle()
            .Which.Should().BeOfType<VisibleStatementSyntax>().Subject;
        visible.Arguments.Should().HaveCount(1);
        visible.SuppressNewline.Should().BeFalse();
    }

    [Fact]
    public void Parse_Visible_MultipleArgs()
    {
        var tree = Parse("HAI 1.2\nVISIBLE \"a\" \"b\" \"c\"\nKTHXBYE");
        var visible = tree.Program.Statements.Should().ContainSingle()
            .Which.Should().BeOfType<VisibleStatementSyntax>().Subject;
        visible.Arguments.Should().HaveCount(3);
    }

    [Fact]
    public void Parse_Visible_SuppressNewline()
    {
        var tree = Parse("HAI 1.2\nVISIBLE \"hello\"!\nKTHXBYE");
        var visible = tree.Program.Statements.Should().ContainSingle()
            .Which.Should().BeOfType<VisibleStatementSyntax>().Subject;
        visible.SuppressNewline.Should().BeTrue();
    }

    [Fact]
    public void Parse_VisibleWithoutArguments_ReportsDiagnostic()
    {
        ParseWithDiagnostics("HAI 1.3\nVISIBLE\nKTHXBYE", out var diagnostics);

        diagnostics.Should().ContainSingle(diagnostic => diagnostic.Id == "LOL1006");
    }

    [Theory]
    [InlineData("NOOB")]
    [InlineData("TROOF")]
    [InlineData("NUMBR")]
    [InlineData("NUMBAR")]
    [InlineData("YARN")]
    public void Parse_TypedDefaultInitialization(string type)
    {
        var tree = ParseWithDiagnostics(
            $"HAI 1.3\nI HAS A value ITZ A {type}\nKTHXBYE",
            out var diagnostics);

        diagnostics.Should().BeEmpty();
        var declaration = tree.Program.Statements.Should().ContainSingle()
            .Which.Should().BeOfType<VariableDeclarationSyntax>().Subject;
        declaration.Initializer.Should().BeOfType<TypeDefaultExpressionSyntax>()
            .Which.TypeToken.Text.Should().Be(type);
    }

    [Fact]
    public void Parse_EmptyFunction_HasValidSourceSpan()
    {
        var tree = ParseWithDiagnostics(
            "HAI 1.3\nHOW IZ I fun\nIF U SAY SO\nKTHXBYE",
            out var diagnostics);

        diagnostics.Should().BeEmpty();
        var function = tree.Program.Statements.Should().ContainSingle()
            .Which.Should().BeOfType<FunctionDeclarationSyntax>().Subject;
        function.Body.Statements.Should().BeEmpty();
        function.Span.Length.Should().BePositive();
    }

    [Fact]
    public void Parse_EscapedInterpolationPrefix_RemainsLiteral()
    {
        var tree = ParseWithDiagnostics(
            "HAI 1.3\nI HAS A value ITZ \"::{name}\"\nKTHXBYE",
            out var diagnostics);

        diagnostics.Should().BeEmpty();
        var declaration = tree.Program.Statements.Should().ContainSingle()
            .Which.Should().BeOfType<VariableDeclarationSyntax>().Subject;
        declaration.Initializer.Should().BeOfType<LiteralExpressionSyntax>()
            .Which.Value.Should().Be(":{name}");
    }

    [Fact]
    public void Parse_Gimmeh()
    {
        var tree = Parse("HAI 1.2\nI HAS A x\nGIMMEH x\nKTHXBYE");
        tree.Program.Statements[1].Should().BeOfType<GimmehStatementSyntax>()
            .Which.NameToken.Text.Should().Be("x");
    }

    [Fact]
    public void Parse_MathExpression()
    {
        var tree = Parse("HAI 1.2\nSUM OF 1 AN 2\nKTHXBYE");
        var exprStmt = tree.Program.Statements.Should().ContainSingle()
            .Which.Should().BeOfType<ExpressionStatementSyntax>().Subject;
        exprStmt.Expression.Should().BeOfType<BinaryExpressionSyntax>();
    }

    [Fact]
    public void Parse_NestedMathExpression()
    {
        var tree = Parse("HAI 1.2\nSUM OF PRODUKT OF 2 AN 3 AN 4\nKTHXBYE");
        var exprStmt = tree.Program.Statements.Should().ContainSingle()
            .Which.Should().BeOfType<ExpressionStatementSyntax>().Subject;
        var outer = exprStmt.Expression.Should().BeOfType<BinaryExpressionSyntax>().Subject;
        outer.Left.Should().BeOfType<BinaryExpressionSyntax>();
    }

    [Fact]
    public void Parse_BothSaem_Comparison()
    {
        var tree = Parse("HAI 1.2\nBOTH SAEM 1 AN 1\nKTHXBYE");
        var exprStmt = tree.Program.Statements.Should().ContainSingle()
            .Which.Should().BeOfType<ExpressionStatementSyntax>().Subject;
        exprStmt.Expression.Should().BeOfType<ComparisonExpressionSyntax>();
    }

    [Fact]
    public void Parse_NotExpression()
    {
        var tree = Parse("HAI 1.2\nNOT WIN\nKTHXBYE");
        var exprStmt = tree.Program.Statements.Should().ContainSingle()
            .Which.Should().BeOfType<ExpressionStatementSyntax>().Subject;
        exprStmt.Expression.Should().BeOfType<UnaryExpressionSyntax>();
    }

    [Fact]
    public void Parse_Smoosh()
    {
        var tree = Parse("HAI 1.2\nSMOOSH \"a\" AN \"b\" MKAY\nKTHXBYE");
        var exprStmt = tree.Program.Statements.Should().ContainSingle()
            .Which.Should().BeOfType<ExpressionStatementSyntax>().Subject;
        var smoosh = exprStmt.Expression.Should().BeOfType<SmooshExpressionSyntax>().Subject;
        smoosh.Operands.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_IfStatement()
    {
        var tree = Parse("""
            HAI 1.2
            WIN
            O RLY?
              YA RLY
                VISIBLE "yes"
              NO WAI
                VISIBLE "no"
            OIC
            KTHXBYE
            """);
        tree.Program.Statements.Should().HaveCount(2); // expression + if
        tree.Program.Statements[1].Should().BeOfType<IfStatementSyntax>();
    }

    [Fact]
    public void Parse_IfStatement_WithMebbe()
    {
        var tree = Parse("""
            HAI 1.2
            WIN
            O RLY?
              YA RLY
                VISIBLE "yes"
              MEBBE FAIL
                VISIBLE "maybe"
              NO WAI
                VISIBLE "no"
            OIC
            KTHXBYE
            """);
        var ifStmt = tree.Program.Statements[1].Should().BeOfType<IfStatementSyntax>().Subject;
        ifStmt.MebbeClauses.Should().HaveCount(1);
        ifStmt.NoWaiBody.Should().NotBeNull();
    }

    [Fact]
    public void Parse_SwitchStatement()
    {
        var tree = Parse("""
            HAI 1.2
            1
            WTF?
              OMG 1
                VISIBLE "one"
                GTFO
              OMG 2
                VISIBLE "two"
                GTFO
              OMGWTF
                VISIBLE "other"
            OIC
            KTHXBYE
            """);
        tree.Program.Statements[1].Should().BeOfType<SwitchStatementSyntax>()
            .Which.OmgClauses.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_Loop()
    {
        var tree = Parse("""
            HAI 1.2
            IM IN YR loop UPPIN YR i TIL BOTH SAEM i AN 10
              VISIBLE i
            IM OUTTA YR loop
            KTHXBYE
            """);
        var loop = tree.Program.Statements.Should().ContainSingle()
            .Which.Should().BeOfType<LoopStatementSyntax>().Subject;
        loop.LabelToken.Text.Should().Be("loop");
        loop.OperationToken!.Kind.Should().Be(SyntaxKind.UppinKeyword);
    }

    [Fact]
    public void Parse_LoopWithReferenceStyleCustomOperation()
    {
        var tree = Parse("""
            HAI 1.2
            IM IN YR loop I IZ bump YR i MKAY TIL BOTH SAEM i AN 2
            IM OUTTA YR loop
            KTHXBYE
            """);
        var loop = tree.Program.Statements.Should().ContainSingle()
            .Which.Should().BeOfType<LoopStatementSyntax>().Subject;
        loop.OperationToken!.Text.Should().Be("bump");
        loop.VariableToken!.Text.Should().Be("i");
    }

    [Fact]
    public void Parse_FunctionDeclaration()
    {
        var tree = Parse("""
            HAI 1.2
            HOW IZ I add YR a AN YR b
              FOUND YR SUM OF a AN b
            IF U SAY SO
            KTHXBYE
            """);
        var func = tree.Program.Statements.Should().ContainSingle()
            .Which.Should().BeOfType<FunctionDeclarationSyntax>().Subject;
        func.NameToken.Text.Should().Be("add");
        func.Parameters.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_FunctionCall()
    {
        var tree = Parse("""
            HAI 1.2
            HOW IZ I add YR a AN YR b
              FOUND YR SUM OF a AN b
            IF U SAY SO
            I IZ add YR 1 AN YR 2 MKAY
            KTHXBYE
            """);
        tree.Program.Statements.Should().HaveCount(2);
        var call = tree.Program.Statements[1].Should().BeOfType<ExpressionStatementSyntax>().Subject;
        call.Expression.Should().BeOfType<FunctionCallExpressionSyntax>();
    }

    [Fact]
    public void Parse_CastExpression()
    {
        var tree = Parse("HAI 1.2\nMAEK 42 A YARN\nKTHXBYE");
        var exprStmt = tree.Program.Statements.Should().ContainSingle()
            .Which.Should().BeOfType<ExpressionStatementSyntax>().Subject;
        exprStmt.Expression.Should().BeOfType<CastExpressionSyntax>();
    }

    [Fact]
    public void Parse_CastStatement()
    {
        var tree = Parse("HAI 1.2\nI HAS A x ITZ 42\nx IS NOW A YARN\nKTHXBYE");
        tree.Program.Statements[1].Should().BeOfType<CastStatementSyntax>();
    }

    [Fact]
    public void Parse_GtfoStatement()
    {
        var tree = Parse("""
            HAI 1.2
            IM IN YR loop
              GTFO
            IM OUTTA YR loop
            KTHXBYE
            """);
        var loop = tree.Program.Statements.Should().ContainSingle()
            .Which.Should().BeOfType<LoopStatementSyntax>().Subject;
        loop.Body.Statements.Should().ContainSingle()
            .Which.Should().BeOfType<GtfoStatementSyntax>();
    }

    [Fact]
    public void Parse_AllOf()
    {
        var tree = Parse("HAI 1.2\nALL OF WIN AN WIN AN WIN MKAY\nKTHXBYE");
        var exprStmt = tree.Program.Statements.Should().ContainSingle()
            .Which.Should().BeOfType<ExpressionStatementSyntax>().Subject;
        var allOf = exprStmt.Expression.Should().BeOfType<AllOfExpressionSyntax>().Subject;
        allOf.Operands.Should().HaveCount(3);
    }

    [Fact]
    public void Parse_AnyOf()
    {
        var tree = Parse("HAI 1.2\nANY OF FAIL AN WIN MKAY\nKTHXBYE");
        var exprStmt = tree.Program.Statements.Should().ContainSingle()
            .Which.Should().BeOfType<ExpressionStatementSyntax>().Subject;
        exprStmt.Expression.Should().BeOfType<AnyOfExpressionSyntax>();
    }

    [Fact]
    public void Parse_Diffrint()
    {
        var tree = Parse("HAI 1.2\nDIFFRINT 1 AN 2\nKTHXBYE");
        var exprStmt = tree.Program.Statements.Should().ContainSingle()
            .Which.Should().BeOfType<ExpressionStatementSyntax>().Subject;
        exprStmt.Expression.Should().BeOfType<DiffrintExpressionSyntax>();
    }
}
