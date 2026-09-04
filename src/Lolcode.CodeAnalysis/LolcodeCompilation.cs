using System.Collections.Immutable;
using System.Text;
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
    /// <remarks>
    /// The DLL, optional PDB, and runtime configuration are replaced as one coordinated
    /// operation. If optional symbols cannot be produced or persisted, emission succeeds
    /// without a PDB and <see cref="EmitResult.PdbPath"/> is <see langword="null"/>. An
    /// unremovable stale PDB is left unreferenced by the PE and reported as a warning.
    /// </remarks>
    public EmitResult Emit(string outputPath, string runtimeAssemblyPath)
        => Emit(outputPath, runtimeAssemblyPath, PhysicalPathEmitFileSystem.Instance);

    internal EmitResult Emit(
        string outputPath,
        string runtimeAssemblyPath,
        IPathEmitFileSystem fileSystem,
        Func<Stream>? pdbStreamFactory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(runtimeAssemblyPath);
        ArgumentNullException.ThrowIfNull(fileSystem);

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
            var pdbPath = Path.ChangeExtension(dllPath, ".pdb");
            var runtimeConfigPath = Path.ChangeExtension(dllPath, ".runtimeconfig.json");

            using var peStream = new MemoryStream();
            using var pdbStream = emitPdb
                ? pdbStreamFactory?.Invoke() ?? new MemoryStream()
                : null;
            var result = EmitCore(
                peStream,
                pdbStream,
                runtimeType,
                assemblyName,
                diagnostics,
                dllPath,
                emitPdb ? pdbPath : null,
                emitPdb ? Path.GetFileName(pdbPath) : null,
                toleratePdbFailure: true);

            var outputDirectory = Path.GetDirectoryName(dllPath);
            if (!string.IsNullOrEmpty(outputDirectory))
                fileSystem.CreateDirectory(outputDirectory);

            var stagedPaths = new List<string>();
            var stagingExceptions = new List<ArtifactStagingException>();

            void TrackStagingArtifacts(Exception exception)
            {
                foreach (var stagingException in GetArtifactStagingExceptions(exception))
                {
                    if (!stagingExceptions.Contains(stagingException))
                        stagingExceptions.Add(stagingException);
                    if (!stagedPaths.Contains(stagingException.ArtifactPath, StringComparer.Ordinal))
                        stagedPaths.Add(stagingException.ArtifactPath);
                }
            }

            try
            {
                string? stagedPdbPath = null;
                if (result.PdbPath != null && pdbStream != null)
                {
                    try
                    {
                        stagedPdbPath = StageStream(fileSystem, pdbStream, pdbPath);
                        stagedPaths.Add(stagedPdbPath);
                    }
                    catch (Exception ex) when (IsPathEmissionFailure(ex))
                    {
                        TrackStagingArtifacts(ex);
                        result = EmitPeWithoutSymbols(
                            peStream,
                            runtimeType,
                            assemblyName,
                            diagnostics,
                            dllPath);
                    }
                }

                var stagedRuntimeConfigPath = StageText(
                    fileSystem,
                    GetRuntimeConfigContents(),
                    runtimeConfigPath);
                stagedPaths.Add(stagedRuntimeConfigPath);

                var stagedPePath = StageStream(fileSystem, peStream, dllPath);
                stagedPaths.Add(stagedPePath);

                string StagePeWithoutSymbols()
                {
                    result = EmitPeWithoutSymbols(
                        peStream,
                        runtimeType,
                        assemblyName,
                        diagnostics,
                        dllPath);
                    var fallbackPath = StageStream(fileSystem, peStream, dllPath);
                    stagedPaths.Add(fallbackPath);
                    return fallbackPath;
                }

                var commitResult = CommitPathArtifacts(
                    fileSystem,
                    dllPath,
                    pdbPath,
                    runtimeConfigPath,
                    stagedPePath,
                    stagedPdbPath,
                    stagedRuntimeConfigPath,
                    StagePeWithoutSymbols);

                var cleanupFailures = commitResult.CleanupFailures.AddRange(
                    CleanupStagedArtifacts(
                        fileSystem,
                        stagedPaths,
                        stagingExceptions));
                var emitDiagnostics = diagnostics;
                foreach (var cleanupFailure in cleanupFailures)
                {
                    emitDiagnostics = emitDiagnostics.Add(Diagnostic.Create(
                        DiagnosticDescriptors.ArtifactCleanupFailed,
                        default,
                        cleanupFailure.Path,
                        cleanupFailure.Exception.Message));
                }

                return new EmitResult(
                    true,
                    emitDiagnostics,
                    dllPath,
                    commitResult.PdbCommitted ? pdbPath : null);
            }
            catch (Exception operationException) when (IsPathEmissionFailure(operationException))
            {
                TrackStagingArtifacts(operationException);
                var cleanupFailures = CleanupStagedArtifacts(
                    fileSystem,
                    stagedPaths,
                    stagingExceptions);
                if (!cleanupFailures.IsEmpty)
                {
                    throw new AggregateException(
                        new[] { operationException }
                            .Concat(cleanupFailures.Select(failure => failure.Exception)));
                }

                throw;
            }
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
            AggregateException or
            System.Security.SecurityException or
            System.Security.Cryptography.CryptographicException;
    }

    private EmitResult EmitPeWithoutSymbols(
        MemoryStream peStream,
        Type runtimeType,
        string assemblyName,
        ImmutableArray<Diagnostic> diagnostics,
        string dllPath)
    {
        peStream.SetLength(0);
        peStream.Position = 0;
        return EmitCore(
            peStream,
            null,
            runtimeType,
            assemblyName,
            diagnostics,
            dllPath,
            pdbPath: null,
            pdbFileName: null);
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
        string? pdbFileName,
        bool toleratePdbFailure = false)
    {
        var tree = SyntaxTrees[0];
        var generator = new CodeGenerator(
            _boundTree!,
            assemblyName,
            runtimeType,
            sourceText: tree.Text,
            sourceFilePath: tree.FilePath);
        var pdbEmitted = false;
        if (toleratePdbFailure && pdbStream != null && pdbFileName != null)
            pdbEmitted = generator.EmitWithOptionalPdb(peStream, pdbStream, pdbFileName);
        else
        {
            generator.Emit(peStream, pdbStream, pdbFileName);
            pdbEmitted = pdbStream != null;
        }

        return new EmitResult(true, diagnostics, outputPath, pdbEmitted ? pdbPath : null);
    }

    private static void ValidateOutputStream(Stream stream, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(stream, parameterName);
        if (!stream.CanWrite)
            throw new ArgumentException("Stream must support writing.", parameterName);
    }

    private static string GetRuntimeConfigContents()
    {
        return """
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
    }

    private static string StageStream(
        IPathEmitFileSystem fileSystem,
        Stream source,
        string destinationPath)
    {
        var temporaryPath = CreateArtifactPath(destinationPath, "tmp");
        try
        {
            source.Position = 0;
            using var destination = fileSystem.CreateNewFile(temporaryPath);
            source.CopyTo(destination);
            return temporaryPath;
        }
        catch (Exception ex) when (IsPathEmissionFailure(ex))
        {
            try
            {
                DeleteIfExists(fileSystem, temporaryPath);
            }
            catch (Exception cleanupException) when (IsPathEmissionFailure(cleanupException))
            {
                throw new ArtifactStagingException(
                    temporaryPath,
                    ex,
                    cleanupException);
            }

            throw;
        }
    }

    private static string StageText(
        IPathEmitFileSystem fileSystem,
        string contents,
        string destinationPath)
    {
        var temporaryPath = CreateArtifactPath(destinationPath, "tmp");
        try
        {
            using var destination = fileSystem.CreateNewFile(temporaryPath);
            using var writer = new StreamWriter(
                destination,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                bufferSize: 1024,
                leaveOpen: false);
            writer.Write(contents);
            return temporaryPath;
        }
        catch (Exception ex) when (IsPathEmissionFailure(ex))
        {
            try
            {
                DeleteIfExists(fileSystem, temporaryPath);
            }
            catch (Exception cleanupException) when (IsPathEmissionFailure(cleanupException))
            {
                throw new ArtifactStagingException(
                    temporaryPath,
                    ex,
                    cleanupException);
            }

            throw;
        }
    }

    private static PathCommitResult CommitPathArtifacts(
        IPathEmitFileSystem fileSystem,
        string dllPath,
        string pdbPath,
        string runtimeConfigPath,
        string stagedPePath,
        string? stagedPdbPath,
        string stagedRuntimeConfigPath,
        Func<string> stagePeWithoutSymbols)
    {
        var targetPaths = new[] { runtimeConfigPath, pdbPath, dllPath };
        var originallyExisted = targetPaths.ToDictionary(
            path => path,
            fileSystem.FileExists,
            StringComparer.Ordinal);
        var backupPaths = new Dictionary<string, string>(StringComparer.Ordinal);
        var pePathToCommit = stagedPePath;
        var pdbCommitted = false;
        var removeUnbackedPdbAfterCommit = false;
        var cleanupFailures = ImmutableArray.CreateBuilder<PathCleanupFailure>();

        void OmitSymbols()
        {
            if (stagedPdbPath == null)
                return;

            DeleteIfExists(fileSystem, stagedPdbPath);
            DeleteIfExists(fileSystem, pePathToCommit);
            stagedPdbPath = null;
            pePathToCommit = stagePeWithoutSymbols();
            pdbCommitted = false;
        }

        try
        {
            foreach (var targetPath in targetPaths)
            {
                if (!originallyExisted[targetPath])
                    continue;

                var backupPath = CreateArtifactPath(targetPath, "bak");
                try
                {
                    fileSystem.MoveFile(targetPath, backupPath, overwrite: false);
                    backupPaths.Add(targetPath, backupPath);
                }
                catch (Exception ex) when (
                    targetPath == pdbPath
                    && IsPathEmissionFailure(ex))
                {
                    // Keep an unmovable old PDB in place until required outputs commit.
                    // A no-debug PE does not reference it, so failed cleanup is safe.
                    if (fileSystem.FileExists(backupPath))
                        backupPaths.Add(targetPath, backupPath);
                    removeUnbackedPdbAfterCommit = fileSystem.FileExists(pdbPath);

                    OmitSymbols();
                }
            }

            fileSystem.MoveFile(stagedRuntimeConfigPath, runtimeConfigPath, overwrite: false);

            if (stagedPdbPath != null)
            {
                try
                {
                    fileSystem.MoveFile(stagedPdbPath, pdbPath, overwrite: false);
                    pdbCommitted = true;
                }
                catch (Exception ex) when (IsPathEmissionFailure(ex))
                {
                    DeleteIfExists(fileSystem, pdbPath);
                    OmitSymbols();
                }
            }

            // The PE is the commit marker: no required artifact replacement follows it.
            fileSystem.MoveFile(pePathToCommit, dllPath, overwrite: false);
        }
        catch (Exception commitException) when (IsPathEmissionFailure(commitException))
        {
            var rollbackException = TryRollbackPathArtifacts(
                fileSystem,
                targetPaths,
                originallyExisted,
                backupPaths);
            if (rollbackException != null)
                throw new AggregateException(commitException, rollbackException);

            throw;
        }

        if (removeUnbackedPdbAfterCommit)
        {
            try
            {
                DeleteIfExists(fileSystem, pdbPath);
            }
            catch (Exception ex) when (IsPathEmissionFailure(ex))
            {
                cleanupFailures.Add(new PathCleanupFailure(pdbPath, ex));
            }
        }

        foreach (var backupPath in backupPaths.Values)
        {
            try
            {
                DeleteIfExists(fileSystem, backupPath);
            }
            catch (Exception ex) when (IsPathEmissionFailure(ex))
            {
                cleanupFailures.Add(new PathCleanupFailure(backupPath, ex));
            }
        }

        return new PathCommitResult(pdbCommitted, cleanupFailures.ToImmutable());
    }

    private static Exception? TryRollbackPathArtifacts(
        IPathEmitFileSystem fileSystem,
        IEnumerable<string> targetPaths,
        IReadOnlyDictionary<string, bool> originallyExisted,
        IReadOnlyDictionary<string, string> backupPaths)
    {
        var failures = new List<Exception>();

        foreach (var targetPath in targetPaths)
        {
            try
            {
                if (backupPaths.TryGetValue(targetPath, out var backupPath))
                    fileSystem.MoveFile(backupPath, targetPath, overwrite: true);
                else if (!originallyExisted[targetPath])
                    DeleteIfExists(fileSystem, targetPath);
            }
            catch (Exception ex) when (IsPathEmissionFailure(ex))
            {
                failures.Add(ex);
            }
        }

        return failures.Count switch
        {
            0 => null,
            1 => failures[0],
            _ => new AggregateException(failures)
        };
    }

    private static ImmutableArray<PathCleanupFailure> CleanupStagedArtifacts(
        IPathEmitFileSystem fileSystem,
        IEnumerable<string> stagedPaths,
        IReadOnlyCollection<ArtifactStagingException> stagingExceptions)
    {
        var failures = ImmutableArray.CreateBuilder<PathCleanupFailure>();

        foreach (var stagedPath in stagedPaths.Distinct(StringComparer.Ordinal))
        {
            try
            {
                DeleteIfExists(fileSystem, stagedPath);
            }
            catch (Exception ex) when (IsPathEmissionFailure(ex))
            {
                var earlierFailures = stagingExceptions
                    .Where(stagingException =>
                        stagingException.ArtifactPath == stagedPath)
                    .Cast<Exception>()
                    .ToArray();
                var cleanupException = earlierFailures.Length == 0
                    ? ex
                    : new AggregateException(earlierFailures.Append(ex));
                failures.Add(new PathCleanupFailure(stagedPath, cleanupException));
            }
        }

        return failures.ToImmutable();
    }

    private static IEnumerable<ArtifactStagingException> GetArtifactStagingExceptions(
        Exception exception)
    {
        if (exception is ArtifactStagingException stagingException)
        {
            yield return stagingException;
            yield break;
        }

        if (exception is AggregateException aggregateException)
        {
            foreach (var innerException in aggregateException.InnerExceptions)
            {
                foreach (var nestedException in GetArtifactStagingExceptions(innerException))
                    yield return nestedException;
            }
        }
        else if (exception.InnerException != null)
        {
            foreach (var nestedException in GetArtifactStagingExceptions(exception.InnerException))
                yield return nestedException;
        }
    }

    private static string CreateArtifactPath(string targetPath, string suffix)
        => $"{targetPath}.{Guid.NewGuid():N}.{suffix}";

    private static void DeleteIfExists(IPathEmitFileSystem fileSystem, string path)
    {
        if (fileSystem.FileExists(path))
            fileSystem.DeleteFile(path);
    }

    private sealed record PathCommitResult(
        bool PdbCommitted,
        ImmutableArray<PathCleanupFailure> CleanupFailures);

    private sealed record PathCleanupFailure(string Path, Exception Exception);

    private sealed class ArtifactStagingException : IOException
    {
        public string ArtifactPath { get; }

        public ArtifactStagingException(
            string artifactPath,
            Exception stagingException,
            Exception cleanupException)
            : base(
                $"Staging '{artifactPath}' failed: {stagingException.Message} "
                + $"The partial artifact could not be removed: {cleanupException.Message}",
                new AggregateException(stagingException, cleanupException))
        {
            ArtifactPath = artifactPath;
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

internal interface IPathEmitFileSystem
{
    bool FileExists(string path);

    void CreateDirectory(string path);

    Stream CreateNewFile(string path);

    void MoveFile(string sourcePath, string destinationPath, bool overwrite);

    void DeleteFile(string path);
}

internal sealed class PhysicalPathEmitFileSystem : IPathEmitFileSystem
{
    public static PhysicalPathEmitFileSystem Instance { get; } = new();

    private PhysicalPathEmitFileSystem()
    {
    }

    public bool FileExists(string path) => File.Exists(path);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public Stream CreateNewFile(string path)
        => new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);

    public void MoveFile(string sourcePath, string destinationPath, bool overwrite)
        => File.Move(sourcePath, destinationPath, overwrite);

    public void DeleteFile(string path) => File.Delete(path);
}
