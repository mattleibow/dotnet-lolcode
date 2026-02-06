using System.Collections.Immutable;
using Lolcode.Compiler.Binding;
using Lolcode.Compiler.Emit;
using Lolcode.Compiler.Syntax;
using Lolcode.Compiler.Text;

namespace Lolcode.Compiler;

/// <summary>
/// Represents the result of a compilation.
/// </summary>
public sealed class CompilationResult
{
    /// <summary>All diagnostics from all phases.</summary>
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    /// <summary>The path to the emitted DLL, if compilation succeeded.</summary>
    public string? OutputPath { get; }

    /// <summary>Whether compilation succeeded (no errors).</summary>
    public bool Success => !Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    internal CompilationResult(ImmutableArray<Diagnostic> diagnostics, string? outputPath)
    {
        Diagnostics = diagnostics;
        OutputPath = outputPath;
    }
}

/// <summary>
/// The main entry point for the LOLCODE compiler.
/// Orchestrates the full pipeline: Lexer → Parser → Binder → Emitter.
/// </summary>
public sealed class Compilation
{
    /// <summary>
    /// Compiles a LOLCODE source file to a .NET assembly.
    /// </summary>
    /// <param name="sourceText">The source code to compile.</param>
    /// <param name="outputPath">The output DLL path.</param>
    /// <param name="runtimeAssemblyPath">Path to Lolcode.Runtime.dll.</param>
    /// <returns>The compilation result with diagnostics.</returns>
    public static CompilationResult Compile(SourceText sourceText, string outputPath, string runtimeAssemblyPath)
    {
        var diagnostics = new DiagnosticBag();

        // Phase 1: Lex
        var lexer = new Lexer(sourceText);
        var tokens = lexer.Tokenize();
        diagnostics.AddRange(lexer.Diagnostics);

        if (diagnostics.HasErrors)
            return new CompilationResult(diagnostics.ToImmutableArray(), null);

        // Phase 2: Parse
        var parser = new Parser(tokens, sourceText);
        var compilationUnit = parser.Parse();
        diagnostics.AddRange(parser.Diagnostics);

        if (diagnostics.HasErrors)
            return new CompilationResult(diagnostics.ToImmutableArray(), null);

        // Phase 3: Bind
        var binder = new Binder(sourceText);
        var boundTree = binder.BindCompilationUnit(compilationUnit);
        diagnostics.AddRange(binder.Diagnostics);

        if (diagnostics.HasErrors)
            return new CompilationResult(diagnostics.ToImmutableArray(), null);

        // Phase 4: Emit
        try
        {
            string assemblyName = Path.GetFileNameWithoutExtension(outputPath);
            var emitter = new Emitter(boundTree, assemblyName, runtimeAssemblyPath);
            string dllPath = emitter.Emit(outputPath);
            return new CompilationResult(diagnostics.ToImmutableArray(), dllPath);
        }
        catch (Exception ex)
        {
            diagnostics.ReportError("LOL9001", default, $"Internal compiler error: {ex.Message}");
            return new CompilationResult(diagnostics.ToImmutableArray(), null);
        }
    }

    /// <summary>
    /// Parses LOLCODE source code and returns the syntax tree with diagnostics.
    /// Useful for testing the lexer and parser without compilation.
    /// </summary>
    public static (CompilationUnitSyntax Tree, ImmutableArray<Diagnostic> Diagnostics) Parse(SourceText sourceText)
    {
        var diagnostics = new DiagnosticBag();

        var lexer = new Lexer(sourceText);
        var tokens = lexer.Tokenize();
        diagnostics.AddRange(lexer.Diagnostics);

        var parser = new Parser(tokens, sourceText);
        var tree = parser.Parse();
        diagnostics.AddRange(parser.Diagnostics);

        return (tree, diagnostics.ToImmutableArray());
    }

    /// <summary>
    /// Lexes LOLCODE source code and returns the token list with diagnostics.
    /// Useful for testing the lexer in isolation.
    /// </summary>
    public static (IReadOnlyList<SyntaxToken> Tokens, ImmutableArray<Diagnostic> Diagnostics) Lex(SourceText sourceText)
    {
        var lexer = new Lexer(sourceText);
        var tokens = lexer.Tokenize();
        return (tokens, lexer.Diagnostics.ToImmutableArray());
    }
}
