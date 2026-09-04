using System.Collections.Immutable;
using Lolcode.CodeAnalysis.Binding;
using Lolcode.CodeAnalysis.BoundTree;
using Lolcode.CodeAnalysis.CodeGen;
using Lolcode.CodeAnalysis.Errors;
using Lolcode.CodeAnalysis.Lowering;
using Lolcode.CodeAnalysis.Syntax;
using Lolcode.Runtime;

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

    /// <summary>
    /// The path to the emitted DLL for path-based emission, or <see langword="null"/>
    /// for stream-based emission.
    /// </summary>
    public string? OutputPath { get; }

    /// <summary>
    /// The path to the emitted PDB for path-based emission, or <see langword="null"/>
    /// for stream-based emission.
    /// </summary>
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
/// and emit with <see cref="Emit(Stream, Stream?)"/>.
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
    /// Emits the compiled assembly to disk and writes a runtime configuration file next to it.
    /// </summary>
    /// <param name="outputPath">The output assembly path.</param>
    /// <param name="runtimeAssemblyPath">
    /// The path to the compatible <c>Lolcode.Runtime</c> assembly referenced by the output.
    /// This parameter is retained for compatibility with file-based compiler hosts.
    /// </param>
    /// <returns>The result of the emission.</returns>
    public EmitResult Emit(string outputPath, string runtimeAssemblyPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(runtimeAssemblyPath);

        var diagnostics = GetDiagnostics();

        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            return new EmitResult(false, diagnostics, null);

        try
        {
            var dllPath = outputPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                ? outputPath
                : Path.ChangeExtension(outputPath, ".dll");
            var assemblyName = Path.GetFileNameWithoutExtension(dllPath);
            var runtimeAssembly = System.Reflection.Assembly.LoadFrom(runtimeAssemblyPath);
            var runtimeType = runtimeAssembly.GetType(typeof(LolRuntime).FullName!, throwOnError: true)!;
            var emitPdb = !string.IsNullOrEmpty(SyntaxTrees[0].FilePath);
            var pdbPath = emitPdb ? Path.ChangeExtension(dllPath, ".pdb") : null;

            using var peStream = new MemoryStream();
            using var pdbStream = emitPdb ? new MemoryStream() : null;
            var result = EmitCore(
                peStream,
                pdbStream,
                runtimeType,
                assemblyName,
                diagnostics,
                dllPath,
                pdbPath,
                pdbPath == null ? null : Path.GetFileName(pdbPath));

            var outputDirectory = Path.GetDirectoryName(dllPath);
            if (!string.IsNullOrEmpty(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            // Write symbols first so a locked PDB cannot leave a newly replaced DLL
            // paired with stale symbols.
            if (pdbStream != null && pdbPath != null)
            {
                pdbStream.Position = 0;
                WriteStreamToFile(pdbStream, pdbPath);
            }

            peStream.Position = 0;
            WriteStreamToFile(peStream, dllPath);

            WriteRuntimeConfig(dllPath);
            return result;
        }
        catch (Exception ex) when (IsPathEmissionFailure(ex))
        {
            var bag = new DiagnosticBag();
            bag.AddRange(diagnostics);
            bag.Report(DiagnosticDescriptors.InternalError, default, ex.Message);
            return new EmitResult(false, bag.ToImmutableArray(), null);
        }
    }

    private static bool IsPathEmissionFailure(Exception exception)
    {
        return exception is
            IOException or
            UnauthorizedAccessException or
            ArgumentException or
            InvalidOperationException or
            NotSupportedException or
            BadImageFormatException or
            TypeLoadException or
            MissingMethodException or
            System.Security.SecurityException or
            System.Security.Cryptography.CryptographicException;
    }

    private static void WriteStreamToFile(Stream source, string destinationPath)
    {
        var temporaryPath = $"{destinationPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            using (var destination = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            {
                source.CopyTo(destination);
            }

            File.Move(temporaryPath, destinationPath, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }

    /// <summary>
    /// Emits the compiled assembly to caller-provided streams without creating files.
    /// </summary>
    /// <param name="peStream">A writable stream that receives the portable executable.</param>
    /// <param name="pdbStream">
    /// An optional writable stream that receives portable PDB symbols.
    /// </param>
    /// <returns>The result of the emission.</returns>
    /// <remarks>
    /// The caller owns both streams. Their positions are advanced but they are not closed.
    /// Runtime references are resolved from the <c>Lolcode.Runtime</c> assembly already
    /// referenced by this compiler.
    /// </remarks>
    public EmitResult Emit(Stream peStream, Stream? pdbStream = null)
    {
        ValidateOutputStream(peStream, nameof(peStream));
        if (pdbStream != null)
            ValidateOutputStream(pdbStream, nameof(pdbStream));
        if (ReferenceEquals(peStream, pdbStream))
            throw new ArgumentException("PE and PDB output streams must be different.", nameof(pdbStream));

        var diagnostics = GetDiagnostics();
        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            return new EmitResult(false, diagnostics, null);

        var assemblyName = $"LolcodeSubmission_{Guid.NewGuid():N}";
        return EmitCore(
            peStream,
            pdbStream,
            typeof(LolRuntime),
            assemblyName,
            diagnostics,
            outputPath: null,
            pdbPath: null,
            pdbFileName: pdbStream == null ? null : $"{assemblyName}.pdb");
    }

    private EmitResult EmitCore(
        Stream peStream,
        Stream? pdbStream,
        Type runtimeType,
        string assemblyName,
        ImmutableArray<Diagnostic> diagnostics,
        string? outputPath,
        string? pdbPath,
        string? pdbFileName)
    {
        var tree = SyntaxTrees[0];
        var generator = new CodeGenerator(
            _boundTree!,
            assemblyName,
            runtimeType,
            sourceText: tree.Text,
            sourceFilePath: tree.FilePath);
        generator.Emit(peStream, pdbStream, pdbFileName);
        return new EmitResult(true, diagnostics, outputPath, pdbPath);
    }

    private static void ValidateOutputStream(Stream stream, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(stream, parameterName);
        if (!stream.CanWrite)
            throw new ArgumentException("Stream must support writing.", parameterName);
    }

    private static void WriteRuntimeConfig(string dllPath)
    {
        var configPath = Path.ChangeExtension(dllPath, ".runtimeconfig.json");
        var config = """
            {
              "runtimeOptions": {
                "tfm": "net10.0",
                "framework": {
                  "name": "Microsoft.NETCore.App",
                  "version": "10.0.0"
                }
              }
            }
            """;
        File.WriteAllText(configPath, config);
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
