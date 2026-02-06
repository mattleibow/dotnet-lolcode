using System.Collections.Immutable;
using Lolcode.CodeAnalysis.Binding;
using Lolcode.CodeAnalysis.BoundTree;
using Lolcode.CodeAnalysis.CodeGen;
using Lolcode.CodeAnalysis.Syntax;
using Lolcode.CodeAnalysis.Text;

namespace Lolcode.CodeAnalysis;

/// <summary>
/// Result of emitting a compilation. Equivalent to Roslyn's EmitResult.
/// </summary>
public sealed class EmitResult
{
    /// <summary>Whether emission succeeded (no errors).</summary>
    public bool Success { get; }

    /// <summary>All diagnostics from all phases.</summary>
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    /// <summary>The path to the emitted DLL, if emission succeeded.</summary>
    public string? OutputPath { get; }

    internal EmitResult(bool success, ImmutableArray<Diagnostic> diagnostics, string? outputPath)
    {
        Success = success;
        Diagnostics = diagnostics;
        OutputPath = outputPath;
    }
}

/// <summary>
/// Immutable compilation unit for LOLCODE. Equivalent to Roslyn's CSharpCompilation.
/// Create with <see cref="Create"/>, inspect with <see cref="GetDiagnostics"/>,
/// emit with <see cref="Emit"/>.
/// </summary>
public sealed class LolcodeCompilation
{
    /// <summary>The syntax trees in this compilation.</summary>
    public ImmutableArray<SyntaxTree> SyntaxTrees { get; }

    private BoundBlockStatement? _boundTree;
    private ImmutableArray<Diagnostic>? _bindDiagnostics;

    private LolcodeCompilation(ImmutableArray<SyntaxTree> syntaxTrees)
        => SyntaxTrees = syntaxTrees;

    /// <summary>Create a compilation from one or more syntax trees.</summary>
    public static LolcodeCompilation Create(params SyntaxTree[] syntaxTrees)
        => new(syntaxTrees.ToImmutableArray());

    /// <summary>Get all diagnostics (syntax + semantic).</summary>
    public ImmutableArray<Diagnostic> GetDiagnostics()
    {
        EnsureBound();
        var builder = ImmutableArray.CreateBuilder<Diagnostic>();
        foreach (var tree in SyntaxTrees)
            builder.AddRange(tree.Diagnostics);
        builder.AddRange(_bindDiagnostics!.Value);
        return builder.ToImmutable();
    }

    /// <summary>Emit compiled assembly to disk.</summary>
    public EmitResult Emit(string outputPath, string runtimeAssemblyPath)
    {
        EnsureBound();
        var diagnostics = GetDiagnostics();

        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            return new EmitResult(false, diagnostics, null);

        try
        {
            var assemblyName = Path.GetFileNameWithoutExtension(outputPath);
            var generator = new CodeGenerator(_boundTree!, assemblyName, runtimeAssemblyPath);
            var dllPath = generator.Emit(outputPath);
            return new EmitResult(true, diagnostics, dllPath);
        }
        catch (Exception ex)
        {
            var bag = new DiagnosticBag();
            bag.AddRange(diagnostics);
            bag.ReportError("LOL9001", default, $"Internal compiler error: {ex.Message}");
            return new EmitResult(false, bag.ToImmutableArray(), null);
        }
    }

    private void EnsureBound()
    {
        if (_bindDiagnostics is not null) return;

        // LOLCODE is single-file, so use first tree
        var tree = SyntaxTrees[0];
        var binder = new Binder(tree.Text);
        _boundTree = binder.BindCompilationUnit(tree.Root);
        _bindDiagnostics = binder.Diagnostics.ToImmutableArray();
    }
}
