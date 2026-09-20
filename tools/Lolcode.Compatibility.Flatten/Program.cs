using System.Text;
using Lolcode.CodeAnalysis.Syntax;

namespace Lolcode.Compatibility.Flatten;

/// <summary>
/// Converts canonical multi-file LOLCODE units into standard single-file
/// fixtures by moving only direct top-level function declarations before the
/// remaining program statements.
/// </summary>
public static class FixtureFlattener
{
    /// <summary>
    /// Flattens the ordered source units into one valid LOLCODE program.
    /// </summary>
    /// <param name="sourcePaths">Canonical source units in statement order.</param>
    /// <returns>The generated single-file standard LOLCODE source.</returns>
    /// <exception cref="InvalidDataException">
    /// Thrown when a unit is not a valid complete program or the HAI versions differ.
    /// </exception>
    public static string Flatten(IReadOnlyList<string> sourcePaths)
    {
        if (sourcePaths.Count == 0)
            throw new InvalidDataException("A flatten manifest must name at least one source unit.");

        Unit[] units = sourcePaths.Select(ParseUnit).ToArray();
        string version = units[0].Version;
        if (units.Any(unit => !string.Equals(unit.Version, version, StringComparison.Ordinal)))
        {
            throw new InvalidDataException(
                $"All source units must use HAI {version}; found incompatible HAI versions.");
        }

        var result = new StringBuilder();
        AppendWithTrailingNewline(result, units[0].Header);
        foreach (Unit unit in units)
        {
            foreach (string declaration in unit.HoistedDeclarations)
                AppendWithTrailingNewline(result, declaration);
        }

        foreach (Unit unit in units)
            AppendWithTrailingNewline(result, unit.Body);

        AppendWithTrailingNewline(result, units[0].Footer);
        return NormalizeLineEndings(result.ToString());
    }

    /// <summary>Loads source file names from a case-local <c>sources.txt</c> manifest.</summary>
    /// <param name="caseDirectory">The fixture directory containing the manifest.</param>
    /// <returns>Absolute source paths in manifest order.</returns>
    public static IReadOnlyList<string> ReadManifest(string caseDirectory)
    {
        string manifest = Path.Combine(caseDirectory, "sources.txt");
        if (!File.Exists(manifest))
            throw new FileNotFoundException("The flatten manifest was not found.", manifest);

        string[] paths = File.ReadAllLines(manifest)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .Select(path => Path.GetFullPath(Path.Combine(caseDirectory, path)))
            .ToArray();
        if (paths.Any(path => !File.Exists(path)))
        {
            string missing = paths.First(path => !File.Exists(path));
            throw new FileNotFoundException("A source listed by sources.txt was not found.", missing);
        }

        return paths;
    }

    /// <summary>Normalizes generated fixture text to LF line endings.</summary>
    public static string NormalizeLineEndings(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private static Unit ParseUnit(string path)
    {
        string source = StripLauncherTrivia(File.ReadAllText(path, new UTF8Encoding(false, true)));
        SyntaxTree tree = SyntaxTree.ParseText(source, path);
        if (!tree.Diagnostics.IsEmpty)
        {
            throw new InvalidDataException(
                $"'{path}' is not a complete valid LOLCODE program:{Environment.NewLine}" +
                string.Join(Environment.NewLine, tree.Diagnostics));
        }

        ProgramStatementSyntax program = tree.Root.Program;
        if (program.VersionToken is null)
            throw new InvalidDataException($"'{path}' must declare a HAI version.");

        SyntaxToken? headerSeparator = tree.Tokens.FirstOrDefault(
            token => token.Position >= program.VersionToken.Span.End &&
                     token.Kind == SyntaxKind.EndOfLineToken);
        if (headerSeparator is null)
            throw new InvalidDataException($"'{path}' must terminate its HAI declaration.");

        int headerEnd = headerSeparator.Position;
        int bodyStart = headerSeparator.Span.End;
        int footerStart = program.KthxbyeKeyword.Position;
        var functions = new List<TextRange>();
        foreach (StatementSyntax statement in program.Statements)
        {
            if (!IsDirectTopLevelFunction(statement))
                continue;

            var function = (FunctionDeclarationSyntax)statement;
            TextRange startStatement = FindLogicalStatement(
                tree.Tokens,
                bodyStart,
                function.NameToken.Position);
            TextRange endStatement = FindLogicalStatement(
                tree.Tokens,
                bodyStart,
                function.EndKeyword.Span.End - 1);
            int start = startStatement.Start;
            int end = endStatement.End;
            functions.Add(new TextRange(start, end));
        }

        return new Unit(
            program.VersionToken.Text,
            source[..headerEnd],
            RenderLogicalStatements(source, tree.Tokens, bodyStart, footerStart, functions),
            RenderLogicalStatements(source, tree.Tokens, footerStart, source.Length),
            functions.Select(range =>
                RenderLogicalStatements(source, tree.Tokens, range.Start, range.End)).ToArray());
    }

    private static bool IsDirectTopLevelFunction(StatementSyntax statement) =>
        statement is FunctionDeclarationSyntax
        {
            Scope: { DirectToken: not null, Slot: null, NameExpression: null } scope,
            Identifier: { DirectToken: not null, Slot: null, NameExpression: null },
        } &&
        string.Equals(scope.DirectToken.Text, "I", StringComparison.Ordinal);

    private static TextRange FindLogicalStatement(
        IReadOnlyList<SyntaxToken> tokens,
        int minimumStart,
        int position)
    {
        int start = minimumStart;
        foreach (SyntaxToken token in tokens)
        {
            if (token.Kind != SyntaxKind.EndOfLineToken)
                continue;

            if (token.Position >= position)
                return new TextRange(start, token.Position);

            start = token.Span.End;
        }

        throw new InvalidDataException(
            $"Could not determine the logical statement containing source position {position}.");
    }

    private static string RenderLogicalStatements(
        string source,
        IReadOnlyList<SyntaxToken> tokens,
        int start,
        int end,
        IReadOnlyList<TextRange>? excludedRanges = null)
    {
        var result = new StringBuilder(end - start);
        int statementStart = start;
        foreach (SyntaxToken token in tokens)
        {
            if (token.Kind != SyntaxKind.EndOfLineToken ||
                token.Position < start ||
                token.Position >= end)
            {
                continue;
            }

            AppendLogicalStatement(source, statementStart, token.Position, excludedRanges, result);
            statementStart = token.Span.End;
        }

        AppendLogicalStatement(source, statementStart, end, excludedRanges, result);
        return result.ToString();
    }

    private static void AppendLogicalStatement(
        string source,
        int start,
        int end,
        IReadOnlyList<TextRange>? excludedRanges,
        StringBuilder result)
    {
        if (excludedRanges?.Any(range => range.Start <= start && end <= range.End) == true)
            return;

        string statement = source[start..end].Trim();
        if (statement.Length == 0)
            return;

        result.Append(statement);
        result.Append('\n');
    }

    private static string StripLauncherTrivia(string source)
    {
        var result = new StringBuilder(source.Length);
        bool atStart = true;
        foreach (string line in source.SplitLines())
        {
            if (atStart && (line.StartsWith("#!", StringComparison.Ordinal) ||
                            line.StartsWith("#:", StringComparison.Ordinal)))
            {
                continue;
            }

            atStart = false;
            result.Append(line);
        }

        return result.ToString();
    }

    private static void AppendWithTrailingNewline(StringBuilder result, string value)
    {
        if (value.Length == 0)
            return;

        result.Append(value);
        if (result[^1] != '\n')
            result.Append('\n');
    }

    private sealed record Unit(
        string Version,
        string Header,
        string Body,
        string Footer,
        IReadOnlyList<string> HoistedDeclarations);

    private readonly record struct TextRange(int Start, int End);

    private static IEnumerable<string> SplitLines(this string value)
    {
        int start = 0;
        for (int index = 0; index < value.Length; index++)
        {
            if (value[index] is not '\n' and not '\r')
                continue;

            int end = index + 1;
            if (value[index] == '\r' && end < value.Length && value[end] == '\n')
                end++;
            yield return value[start..end];
            start = end;
            index = end - 1;
        }

        if (start < value.Length)
            yield return value[start..];
    }
}

internal static class Program
{
    private static int Main(string[] args)
    {
        bool check = args.Length == 1 && args[0] == "--check";
        if (args.Length != 0 && !check)
        {
            Console.Error.WriteLine("Usage: Lolcode.Compatibility.Flatten [--check]");
            return 2;
        }

        string root = Path.Combine(Directory.GetCurrentDirectory(), "tests", "Compatibility");
        string[] manifests = Directory.EnumerateFiles(root, "sources.txt", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var stale = new List<string>();
        foreach (string manifest in manifests)
        {
            string directory = Path.GetDirectoryName(manifest)!;
            string generated = FixtureFlattener.Flatten(FixtureFlattener.ReadManifest(directory));
            string target = Path.Combine(directory, "test.lol");
            if (File.Exists(target) &&
                FixtureFlattener.NormalizeLineEndings(
                    File.ReadAllText(target, new UTF8Encoding(false, true))) == generated)
                continue;

            if (check)
            {
                stale.Add(Path.GetRelativePath(Directory.GetCurrentDirectory(), target));
                continue;
            }

            File.WriteAllText(target, generated, new UTF8Encoding(false));
            Console.WriteLine($"Generated {Path.GetRelativePath(Directory.GetCurrentDirectory(), target)}");
        }

        if (stale.Count == 0)
            return 0;

        Console.Error.WriteLine("Generated flattened fixtures are stale:");
        foreach (string path in stale)
            Console.Error.WriteLine($"  {path}");
        return 1;
    }
}
