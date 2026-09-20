using System.Text;
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
            "var source = \"\"\"HAI 1.2\nVISIBLE \\\"raw\\\"\nKTHXBYE\"\"\";",
            """var source = "HAI 1.2\nVISIBLE \"escaped\"\nKTHXBYE";""",
            """var source = "\u0048AI 1.2\nKTHXBYE";""",
            "var source = $$\"\"\"HAI {{version}}\nVISIBLE \\\"interpolated\\\"\nKTHXBYE\"\"\";",
            """var source = "HAI 1.2, VISIBLE \"comma\", KTHXBYE";""",
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
        var literals = new List<CSharpStringLiteral>();
        for (int index = 0; index < source.Length;)
        {
            if (StartsLineComment(source, index))
            {
                index = SkipToLineEnd(source, index + 2);
                continue;
            }

            if (StartsBlockComment(source, index))
            {
                index = SkipBlockComment(source, index + 2);
                continue;
            }

            if (source[index] == '\'')
            {
                index = SkipCharacterLiteral(source, index + 1);
                continue;
            }

            if (TryReadStringLiteral(source, index, out CSharpStringLiteral? literal) && literal is not null)
            {
                literals.Add(literal);
                index = literal.End;
                continue;
            }

            index++;
        }

        return literals;
    }

    internal static IEnumerable<string> FindViolations(string path, string source)
    {
        IReadOnlyList<MethodSpan> methods = FindMethods(source);
        foreach (CSharpStringLiteral literal in ExtractStringLiterals(source)
                     .Where(literal => IsCompleteProgram(literal.Value)))
        {
            MethodSpan? method = methods.SingleOrDefault(candidate =>
                candidate.Start <= literal.Start && literal.End <= candidate.End);
            if (method is not null && HasExplicitException(source, method))
                continue;

            int line = source.AsSpan(0, literal.Start).Count('\n') + 1;
            string methodName = method?.Name ?? "<no containing method>";
            yield return $"{path}({line}): complete inline LOLCODE program in {methodName} needs [InlineLolcodeProgramException(\"reason\")].";
        }
    }

    internal static bool IsCompleteProgram(string value) =>
        ProgramStartRegex().IsMatch(value) && ProgramEndRegex().IsMatch(value);

    private static bool TryReadStringLiteral(string source, int start, out CSharpStringLiteral? literal)
    {
        literal = null;
        int cursor = start;
        while (cursor < source.Length && (source[cursor] == '$' || source[cursor] == '@'))
            cursor++;

        if (cursor >= source.Length || source[cursor] != '"')
            return false;

        bool verbatim = source.AsSpan(start, cursor - start).Contains('@');
        int quoteCount = CountQuotes(source, cursor);
        if (quoteCount >= 3 && !verbatim)
        {
            int contentStart = cursor + quoteCount;
            int close = source.IndexOf(new string('"', quoteCount), contentStart, StringComparison.Ordinal);
            if (close < 0)
                return false;

            literal = new CSharpStringLiteral(
                source[contentStart..close],
                start,
                close + quoteCount);
            return true;
        }

        var value = new StringBuilder();
        cursor++;
        while (cursor < source.Length)
        {
            char character = source[cursor++];
            if (character == '"')
            {
                if (verbatim && cursor < source.Length && source[cursor] == '"')
                {
                    value.Append('"');
                    cursor++;
                    continue;
                }

                literal = new CSharpStringLiteral(value.ToString(), start, cursor);
                return true;
            }

            if (!verbatim && character == '\\' && cursor < source.Length)
            {
                value.Append(ReadEscape(source, ref cursor));
                continue;
            }

            value.Append(character);
        }

        return false;
    }

    private static IReadOnlyList<MethodSpan> FindMethods(string source)
    {
        string code = ReplaceStringsAndCommentsWithSpaces(source);
        var methods = new List<MethodSpan>();
        foreach (Match match in MethodDeclarationRegex().Matches(code))
        {
            int openBrace = code.IndexOf('{', match.Index, match.Length);
            int closeBrace = FindClosingBrace(code, openBrace);
            if (closeBrace >= 0)
                methods.Add(new MethodSpan(match.Index, closeBrace + 1, match.Groups["name"].Value));
        }

        return methods;
    }

    private static bool HasExplicitException(string source, MethodSpan method) =>
        InlineExceptionRegex().IsMatch(source[method.Start..method.End]);

    private static string ReplaceStringsAndCommentsWithSpaces(string source)
    {
        var code = source.ToCharArray();
        foreach (CSharpStringLiteral literal in ExtractStringLiterals(source))
        {
            for (int index = literal.Start; index < literal.End; index++)
                if (code[index] != '\n' && code[index] != '\r')
                    code[index] = ' ';
        }

        for (int index = 0; index < code.Length;)
        {
            if (StartsLineComment(source, index))
            {
                int end = SkipToLineEnd(source, index + 2);
                for (int position = index; position < end; position++)
                    code[position] = ' ';
                index = end;
                continue;
            }

            if (StartsBlockComment(source, index))
            {
                int end = SkipBlockComment(source, index + 2);
                for (int position = index; position < end; position++)
                    if (code[position] != '\n' && code[position] != '\r')
                        code[position] = ' ';
                index = end;
                continue;
            }

            index++;
        }

        return new string(code);
    }

    private static int FindClosingBrace(string source, int openBrace)
    {
        int depth = 0;
        for (int index = openBrace; index < source.Length; index++)
        {
            if (source[index] == '{')
                depth++;
            else if (source[index] == '}' && --depth == 0)
                return index;
        }

        return -1;
    }

    private static int CountQuotes(string source, int start)
    {
        int count = 0;
        while (start + count < source.Length && source[start + count] == '"')
            count++;
        return count;
    }

    private static char ReadEscape(string source, ref int cursor)
    {
        char escape = source[cursor++];
        return escape switch
        {
            'n' => '\n',
            'r' => '\r',
            't' => '\t',
            'u' => ReadUnicodeEscape(source, ref cursor, 4),
            'U' => ReadUnicodeEscape(source, ref cursor, 8),
            'x' => ReadVariableLengthHexEscape(source, ref cursor),
            _ => escape,
        };
    }

    private static char ReadUnicodeEscape(string source, ref int cursor, int digits)
    {
        if (cursor + digits > source.Length ||
            !int.TryParse(source.AsSpan(cursor, digits), System.Globalization.NumberStyles.AllowHexSpecifier,
                System.Globalization.CultureInfo.InvariantCulture, out int value))
            return '\0';

        cursor += digits;
        return char.ConvertFromUtf32(value)[0];
    }

    private static char ReadVariableLengthHexEscape(string source, ref int cursor)
    {
        int start = cursor;
        while (cursor < source.Length && cursor - start < 4 && Uri.IsHexDigit(source[cursor]))
            cursor++;

        return cursor == start ||
            !int.TryParse(source.AsSpan(start, cursor - start),
                System.Globalization.NumberStyles.AllowHexSpecifier,
                System.Globalization.CultureInfo.InvariantCulture,
                out int value)
            ? '\0'
            : (char)value;
    }

    private static bool StartsLineComment(string source, int index) =>
        index + 1 < source.Length && source[index] == '/' && source[index + 1] == '/';

    private static bool StartsBlockComment(string source, int index) =>
        index + 1 < source.Length && source[index] == '/' && source[index + 1] == '*';

    private static int SkipToLineEnd(string source, int index)
    {
        while (index < source.Length && source[index] is not '\r' and not '\n')
            index++;
        return index;
    }

    private static int SkipBlockComment(string source, int index)
    {
        int close = source.IndexOf("*/", index, StringComparison.Ordinal);
        return close < 0 ? source.Length : close + 2;
    }

    private static int SkipCharacterLiteral(string source, int index)
    {
        while (index < source.Length)
        {
            if (source[index++] == '\\' && index < source.Length)
            {
                index++;
                continue;
            }

            if (source[index - 1] == '\'')
                break;
        }

        return index;
    }

    [GeneratedRegex(@"(?m)^[ \t]*(?:\[[^\]]+\][ \t\r\n]*)*(?:public|private|internal|protected)\s+(?:static\s+)?(?:async\s+)?(?:[\w<>\[\],?.]+\s+)+(?<name>\w+)\s*\([^{};]*\)\s*\{")]
    private static partial Regex MethodDeclarationRegex();

    [GeneratedRegex(@"\[InlineLolcodeProgramException\(\s*""[^""]+\s*""\)\]", RegexOptions.CultureInvariant)]
    private static partial Regex InlineExceptionRegex();

    [GeneratedRegex(@"(?m)^[ \t]*HAI\b")]
    private static partial Regex ProgramStartRegex();

    [GeneratedRegex(@"\bKTHXBYE\b")]
    private static partial Regex ProgramEndRegex();

    private sealed record MethodSpan(int Start, int End, string Name);
}
