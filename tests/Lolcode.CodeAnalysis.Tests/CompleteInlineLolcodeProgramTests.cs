using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text.RegularExpressions;

namespace Lolcode.CodeAnalysis.Tests;

public sealed partial class CompleteInlineLolcodeProgramTests
{
    [Fact]
    public void Complete_inline_programs_require_a_method_scoped_exception()
    {
        string repositoryRoot = FindRepositoryRoot();
        string[] testRoots =
        [
            Path.Combine(repositoryRoot, "tests", "Lolcode.EndToEnd.Tests"),
            Path.Combine(repositoryRoot, "tests", "Lolcode.CodeAnalysis.Tests"),
            Path.Combine(repositoryRoot, "tests", "Lolcode.Web.Tests"),
        ];

        string[] violations = testRoots
            .SelectMany(root => Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            .SelectMany(path => CSharpInlineLolcodeProgramScanner.FindViolations(path, File.ReadAllText(path)))
            .ToArray();

        violations.Should().BeEmpty(string.Join(Environment.NewLine, violations));
    }

    [Fact]
    [InlineLolcodeProgramException("Exercises the literal scanner with synthetic C# source.")]
    public void Scanner_detects_supported_complete_program_literal_forms()
    {
        string[] sources =
        [
            "var source = \"\"\"\nHAI 1.2\nVISIBLE \"raw\"\nKTHXBYE\n\"\"\";",
            """var source = "HAI 1.2\nVISIBLE \"escaped\"\nKTHXBYE";""",
            "var source = @\"HAI 1.2\nKTHXBYE\";",
            """var source = "\u0048AI 1.2\nKTHXBYE";""",
            "var source = $$\"\"\"\nHAI {{version}}\nVISIBLE \"interpolated\"\nKTHXBYE\n\"\"\";",
            """var source = "HAI 1.2, VISIBLE \"comma\", KTHXBYE";""",
            """var source = "\uFEFFHAI 1.2\nVISIBLE \"\uD83D\uDE00\"\nKTHXBYE";""",
        ];

        foreach (string source in sources)
        {
            CSharpInlineLolcodeProgramScanner.ExtractStringLiterals(source)
                .Select(literal => literal.Value)
                .Where(CSharpInlineLolcodeProgramScanner.IsCompleteProgram)
                .Should()
                .ContainSingle();
        }
    }

    [Fact]
    [InlineLolcodeProgramException("Exercises rejection of a synthetic unapproved C# program literal.")]
    public void Scanner_rejects_an_unapproved_complete_program()
    {
        const string source = """
            public class C
            {
                public void M()
                {
                    var source = "HAI 1.2\nKTHXBYE";
                }
            }
            """;

        CSharpInlineLolcodeProgramScanner.FindViolations("synthetic.cs", source)
            .Should()
            .ContainSingle()
            .Which.Should()
            .Contain("M");
    }

    [Fact]
    [InlineLolcodeProgramException("Exercises syntax-aware interpolation scanning with synthetic C# source.")]
    public void Scanner_detects_programs_in_interpolated_strings_with_nested_quoted_expressions()
    {
        const string source = """"
            public class C
            {
                public void M()
                {
                    var source = $"HAI 1.2\nVISIBLE {42.ToString("D")}\nKTHXBYE";
                }
            }
            """";

        CSharpInlineLolcodeProgramScanner.FindViolations("synthetic.cs", source)
            .Should()
            .ContainSingle()
            .Which.Should()
            .Contain("M");
    }

    [Fact]
    [InlineLolcodeProgramException("Exercises syntax-aware attribute detection with synthetic C# source.")]
    public void Scanner_only_recognizes_actual_method_attributes()
    {
        const string source = """"
            public class C
            {
                // [InlineLolcodeProgramException("comment")]
                public void CommentedAttribute()
                {
                    var source = "HAI 1.2\nKTHXBYE";
                }

                public void RawStringAttribute()
                {
                    var attribute = """[InlineLolcodeProgramException("raw")]""";
                    var source = "HAI 1.2\nKTHXBYE";
                }

                [Other.InlineLolcodeProgramException("unrelated")]
                public void QualifiedAttribute()
                {
                    var source = "HAI 1.2\nKTHXBYE";
                }

                [InlineLolcodeProgramException("actual")]
                public string ExpressionBodied() => "HAI 1.2\nKTHXBYE";

                public void ContainsAnnotatedLocalFunction()
                {
                    [InlineLolcodeProgramException("actual local")]
                    string Local() => "HAI 1.2\nKTHXBYE";
                }

                public void CharacterLiteral()
                {
                    var closeBrace = '}';
                    var source = "HAI 1.2\nKTHXBYE";
                }
            }
            """";

        string[] violations = CSharpInlineLolcodeProgramScanner.FindViolations("synthetic.cs", source).ToArray();

        violations.Should().HaveCount(4);
        violations.Should().Contain(violation => violation.Contains("CommentedAttribute"));
        violations.Should().Contain(violation => violation.Contains("RawStringAttribute"));
        violations.Should().Contain(violation => violation.Contains("QualifiedAttribute"));
        violations.Should().Contain(violation => violation.Contains("CharacterLiteral"));
        violations.Should().NotContain(violation => violation.Contains("ExpressionBodied"));
        violations.Should().NotContain(violation => violation.Contains("Local"));
    }

    private static string FindRepositoryRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "dotnet-lolcode.slnx")))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}

internal sealed record CSharpStringLiteral(string Value, int Start, int End);

internal static partial class CSharpInlineLolcodeProgramScanner
{
    internal static IReadOnlyList<CSharpStringLiteral> ExtractStringLiterals(string source)
    {
        SyntaxNode root = Parse(source).GetRoot();
        return root.DescendantNodes()
            .Select(node => node switch
            {
                LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression } literal =>
                    new CSharpStringLiteral(literal.Token.ValueText, literal.SpanStart, literal.Span.End),
                InterpolatedStringExpressionSyntax interpolated =>
                    new CSharpStringLiteral(GetStaticText(interpolated), interpolated.SpanStart, interpolated.Span.End),
                _ => null,
            })
            .OfType<CSharpStringLiteral>()
            .ToArray();
    }

    internal static IEnumerable<string> FindViolations(string path, string source)
    {
        SyntaxTree tree = Parse(source);
        SyntaxNode root = tree.GetRoot();
        foreach (SyntaxNode node in root.DescendantNodes().Where(IsStringExpression))
        {
            CSharpStringLiteral literal = CreateLiteral(node);
            if (!IsCompleteProgram(literal.Value))
                continue;

            SyntaxNode? method = node.AncestorsAndSelf()
                .FirstOrDefault(candidate => candidate is MethodDeclarationSyntax or LocalFunctionStatementSyntax);
            if (method is not null && HasExplicitException(method))
                continue;

            int line = tree.GetLineSpan(new TextSpan(literal.Start, 0)).StartLinePosition.Line + 1;
            string methodName = GetMethodName(method);
            yield return $"{path}({line}): complete inline LOLCODE program in {methodName} needs [InlineLolcodeProgramException(\"reason\")].";
        }
    }

    internal static bool IsCompleteProgram(string value) =>
        ProgramStartRegex().IsMatch(value.TrimStart('\uFEFF')) && ProgramEndRegex().IsMatch(value);

    private static SyntaxTree Parse(string source) =>
        CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));

    private static bool IsStringExpression(SyntaxNode node) =>
        node is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression } or
            InterpolatedStringExpressionSyntax;

    private static CSharpStringLiteral CreateLiteral(SyntaxNode node) => node switch
    {
        LiteralExpressionSyntax literal =>
            new CSharpStringLiteral(literal.Token.ValueText, literal.SpanStart, literal.Span.End),
        InterpolatedStringExpressionSyntax interpolated =>
            new CSharpStringLiteral(GetStaticText(interpolated), interpolated.SpanStart, interpolated.Span.End),
        _ => throw new ArgumentOutOfRangeException(nameof(node)),
    };

    private static string GetStaticText(InterpolatedStringExpressionSyntax interpolated) =>
        string.Concat(interpolated.Contents.OfType<InterpolatedStringTextSyntax>()
            .Select(text => text.TextToken.ValueText));

    private static bool HasExplicitException(SyntaxNode method) =>
        GetAttributeLists(method).SelectMany(list => list.Attributes).Any(attribute =>
            attribute.Name is IdentifierNameSyntax { Identifier.ValueText: "InlineLolcodeProgramException" } &&
            attribute.ArgumentList?.Arguments is [{ Expression: LiteralExpressionSyntax literal }]
            && literal.IsKind(SyntaxKind.StringLiteralExpression)
            && !string.IsNullOrWhiteSpace(literal.Token.ValueText));

    private static IEnumerable<AttributeListSyntax> GetAttributeLists(SyntaxNode method) => method switch
    {
        MethodDeclarationSyntax declaration => declaration.AttributeLists,
        LocalFunctionStatementSyntax declaration => declaration.AttributeLists,
        _ => [],
    };

    private static string GetMethodName(SyntaxNode? method) => method switch
    {
        MethodDeclarationSyntax declaration => declaration.Identifier.ValueText,
        LocalFunctionStatementSyntax declaration => declaration.Identifier.ValueText,
        _ => "<no containing method>",
    };

    [GeneratedRegex(@"(?m)^[ \t]*HAI\b")]
    private static partial Regex ProgramStartRegex();

    [GeneratedRegex(@"\bKTHXBYE\b")]
    private static partial Regex ProgramEndRegex();
}
