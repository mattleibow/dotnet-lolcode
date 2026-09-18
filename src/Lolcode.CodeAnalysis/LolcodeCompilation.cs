using System.Collections.Immutable;
using Lolcode.CodeAnalysis.Binding;
using Lolcode.CodeAnalysis.BoundTree;
using Lolcode.CodeAnalysis.CodeGen;
using Lolcode.CodeAnalysis.Errors;
using Lolcode.CodeAnalysis.Lowering;
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

    /// <summary>The path to the emitted PDB, if debug symbols were generated.</summary>
    public string? PdbPath { get; }

    internal EmitResult(bool success, ImmutableArray<Diagnostic> diagnostics, string? outputPath, string? pdbPath = null)
    {
        Success = success;
        Diagnostics = diagnostics;
        OutputPath = outputPath;
        PdbPath = pdbPath;
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

    /// <summary>
    /// Emits a compiled executable or class library to disk.
    /// </summary>
    /// <param name="outputPath">The path for the emitted assembly.</param>
    /// <param name="runtimeAssemblyPath">The path to <c>Lolcode.Runtime.dll</c>.</param>
    /// <param name="outputType">
    /// The MSBuild output type. Specify <c>Library</c> to emit a class library;
    /// all other values emit an executable.
    /// </param>
    public EmitResult Emit(
        string outputPath,
        string runtimeAssemblyPath,
        string outputType = "Exe",
        string? libraryTypeName = null)
        => Emit(outputPath, runtimeAssemblyPath, referenceAssemblyPaths: null, outputType, libraryTypeName);

    /// <summary>
    /// Emits a compiled executable or class library to disk using the supplied target reference assemblies.
    /// </summary>
    /// <param name="outputPath">The path for the emitted assembly.</param>
    /// <param name="runtimeAssemblyPath">The path to <c>Lolcode.Runtime.dll</c>.</param>
    /// <param name="referenceAssemblyPaths">
    /// Resolved reference assembly paths for the target framework. When omitted, the compiler uses the
    /// runtime assemblies from the current runtime.
    /// </param>
    /// <param name="outputType">
    /// The MSBuild output type. Specify <c>Library</c> to emit a class library;
    /// all other values emit an executable.
    /// </param>
    /// <param name="libraryTypeName">
    /// The fully qualified CLR type name for a library's export container. When omitted,
    /// libraries use <c>LolcodeExports</c>; executables always use <c>Program</c>.
    /// </param>
    public EmitResult Emit(
        string outputPath,
        string runtimeAssemblyPath,
        IEnumerable<string>? referenceAssemblyPaths,
        string outputType = "Exe",
        string? libraryTypeName = null)
    {
        EnsureBound();
        var diagnostics = GetDiagnostics();

        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            return new EmitResult(false, diagnostics, null);

        try
        {
            var assemblyName = Path.GetFileNameWithoutExtension(outputPath);
            var tree = SyntaxTrees[0];
            bool isLibrary = string.Equals(outputType, "Library", StringComparison.OrdinalIgnoreCase);
            var generator = new CodeGenerator(_boundTree!, assemblyName, runtimeAssemblyPath,
                referenceAssemblyPaths,
                sourceText: tree.Text, sourceFilePath: tree.FilePath,
                isLibrary: isLibrary,
                libraryTypeName: isLibrary && !string.IsNullOrWhiteSpace(libraryTypeName)
                    ? libraryTypeName
                    : null);
            var dllPath = generator.Emit(outputPath);
            return new EmitResult(true, diagnostics, dllPath);
        }
        catch (Exception ex)
        {
            var bag = new DiagnosticBag();
            bag.AddRange(diagnostics);
            bag.Report(DiagnosticDescriptors.InternalError, default, ex.Message);
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

        // Lower the bound tree (simplify for code generation)
        _boundTree = Lowerer.Lower(_boundTree);
    }
}
