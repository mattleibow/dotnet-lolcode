using System.Collections.Immutable;
using Lolcode.CodeAnalysis.Text;

namespace Lolcode.CodeAnalysis.Syntax;

/// <summary>
/// Immutable container for a parsed syntax tree.
/// Equivalent to Roslyn's SyntaxTree.
/// </summary>
public sealed class SyntaxTree
{
    /// <summary>The source text that was parsed.</summary>
    public SourceText Text { get; }

    /// <summary>The root compilation unit node.</summary>
    public CompilationUnitSyntax Root { get; }

    /// <summary>All diagnostics from lexing and parsing.</summary>
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    /// <summary>Optional file path for this syntax tree.</summary>
    public string? FilePath { get; }

    private SyntaxTree(SourceText text, CompilationUnitSyntax root,
                       ImmutableArray<Diagnostic> diagnostics, string? filePath)
    {
        Text = text;
        Root = root;
        Diagnostics = diagnostics;
        FilePath = filePath;
    }

    /// <summary>Parse source text into a syntax tree.</summary>
    /// <param name="text">The source text to parse.</param>
    /// <param name="filePath">
    /// The source path used for diagnostics and symbols, or the source text's file name when omitted.
    /// </param>
    public static SyntaxTree ParseText(SourceText text, string? filePath = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        string effectiveFilePath = filePath ?? text.FileName;
        if (!string.Equals(text.FileName, effectiveFilePath, StringComparison.Ordinal))
            text = SourceText.From(text.ToString(), effectiveFilePath);
        var lexer = new Lexer(text);
        var tokens = lexer.Tokenize();

        var parser = new Parser(tokens, text);
        var root = parser.Parse();

        var diagnostics = lexer.Diagnostics
            .Concat(parser.Diagnostics)
            .ToImmutableArray();

        return new SyntaxTree(text, root, diagnostics, effectiveFilePath);
    }

    /// <summary>Parse source code string into a syntax tree.</summary>
    public static SyntaxTree ParseText(string text, string? filePath = null) =>
        ParseText(SourceText.From(text, filePath ?? ""), filePath);

    /// <summary>Load and parse a source file, preserving its path on the source text and syntax tree.</summary>
    public static SyntaxTree Load(string path) =>
        ParseText(SourceText.From(File.ReadAllText(path), path), path);
}
