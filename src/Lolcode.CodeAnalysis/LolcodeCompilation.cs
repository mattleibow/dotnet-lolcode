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
/// and emit with <see cref="Emit(Stream, Stream?, CancellationToken)"/>.
/// </summary>
public sealed class LolcodeCompilation
{
    /// <summary>The syntax trees in this compilation.</summary>
    public ImmutableArray<SyntaxTree> SyntaxTrees { get; }

    private readonly object _bindingLock = new();
    private readonly string _inMemoryAssemblyName = $"LolcodeSubmission_{Guid.NewGuid():N}";
    private BindingResult? _bindingResult;

    private LolcodeCompilation(ImmutableArray<SyntaxTree> syntaxTrees)
        => SyntaxTrees = syntaxTrees;

    /// <summary>Create a compilation from one or more syntax trees.</summary>
    public static LolcodeCompilation Create(params SyntaxTree[] syntaxTrees)
        => new(syntaxTrees.ToImmutableArray());

    /// <summary>Gets all syntax and semantic diagnostics.</summary>
    /// <param name="cancellationToken">A token checked before and after binding.</param>
    public ImmutableArray<Diagnostic> GetDiagnostics(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var bindingResult = EnsureBound();
        cancellationToken.ThrowIfCancellationRequested();
        var builder = ImmutableArray.CreateBuilder<Diagnostic>();
        foreach (var tree in SyntaxTrees)
        {
            cancellationToken.ThrowIfCancellationRequested();
            builder.AddRange(tree.Diagnostics);
        }
        builder.AddRange(bindingResult.Diagnostics);
        return builder.ToImmutable();
    }

    /// <summary>Emits the compiled assembly to disk.</summary>
    /// <param name="outputPath">The output assembly path.</param>
    /// <param name="runtimeAssemblyPath">
    /// The path to the compatible <c>Lolcode.Runtime</c> assembly referenced by the output.
    /// This parameter is retained for compatibility with file-based compiler hosts.
    /// </param>
    /// <returns>The result of the emission.</returns>
    /// <remarks>
    /// Executables write a runtime configuration file next to the assembly. Libraries do
    /// not have an entry point or runtime configuration file.
    /// </remarks>
    public EmitResult Emit(string outputPath, string runtimeAssemblyPath)
        => Emit(outputPath, runtimeAssemblyPath, PhysicalPathEmitFileSystem.Instance);

    /// <summary>
    /// Emits a compiled executable or class library to disk.
    /// </summary>
    /// <param name="outputPath">The output assembly path.</param>
    /// <param name="runtimeAssemblyPath">The path to <c>Lolcode.Runtime.dll</c>.</param>
    /// <param name="outputType">
    /// The MSBuild output type. Specify <c>Library</c> to emit a class library; all other
    /// values emit an executable.
    /// </param>
    /// <param name="libraryTypeName">
    /// The fully qualified CLR type name for a library export container. When omitted,
    /// libraries use <c>LolcodeExports</c>; executables always use <c>Program</c>.
    /// </param>
    /// <returns>The result of the emission.</returns>
    public EmitResult Emit(
        string outputPath,
        string runtimeAssemblyPath,
        string outputType,
        string? libraryTypeName = null)
        => Emit(
            outputPath,
            runtimeAssemblyPath,
            referenceAssemblyPaths: null,
            outputType: outputType,
            libraryTypeName: libraryTypeName);

    /// <summary>
    /// Emits a compiled executable or class library using the supplied target reference assemblies.
    /// </summary>
    /// <param name="outputPath">The output assembly path.</param>
    /// <param name="runtimeAssemblyPath">The path to <c>Lolcode.Runtime.dll</c>.</param>
    /// <param name="referenceAssemblyPaths">Resolved target framework reference assembly paths.</param>
    /// <param name="outputType">
    /// The MSBuild output type. Specify <c>Library</c> to emit a class library; all other
    /// values emit an executable.
    /// </param>
    /// <param name="libraryTypeName">
    /// The fully qualified CLR type name for a library export container. When omitted,
    /// libraries use <c>LolcodeExports</c>; executables always use <c>Program</c>.
    /// </param>
    /// <returns>The result of the emission.</returns>
    public EmitResult Emit(
        string outputPath,
        string runtimeAssemblyPath,
        IEnumerable<string>? referenceAssemblyPaths,
        string outputType = "Exe",
        string? libraryTypeName = null)
        => Emit(
            outputPath,
            runtimeAssemblyPath,
            PhysicalPathEmitFileSystem.Instance,
            referenceAssemblyPaths: referenceAssemblyPaths,
            outputType: outputType,
            libraryTypeName: libraryTypeName);

    internal EmitResult Emit(
        string outputPath,
        string runtimeAssemblyPath,
        IPathEmitFileSystem fileSystem,
        Func<Stream>? pdbStreamFactory = null,
        IEnumerable<string>? referenceAssemblyPaths = null,
        string outputType = "Exe",
        string? libraryTypeName = null)
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
            bool isLibrary = string.Equals(outputType, "Library", StringComparison.OrdinalIgnoreCase);
            var emitPdb = SyntaxTrees.Any(tree => !string.IsNullOrEmpty(tree.FilePath));
            var pdbPath = Path.ChangeExtension(dllPath, ".pdb");
            var runtimeConfigPath = Path.ChangeExtension(dllPath, ".runtimeconfig.json");

            using var peStream = new MemoryStream();
            using var pdbStream = emitPdb
                ? pdbStreamFactory?.Invoke() ?? new MemoryStream()
                : null;
            var result = EmitCore(
                peStream,
                pdbStream,
                runtimeAssemblyPath,
                assemblyName,
                diagnostics,
                dllPath,
                emitPdb ? pdbPath : null,
                emitPdb ? Path.GetFileName(pdbPath) : null,
                toleratePdbFailure: true,
                referenceAssemblyPaths: referenceAssemblyPaths,
                isLibrary: isLibrary,
                libraryTypeName: libraryTypeName);
            if (!result.Success)
                return result;

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
                            runtimeAssemblyPath,
                            assemblyName,
                            diagnostics,
                            dllPath,
                            referenceAssemblyPaths,
                            isLibrary,
                            libraryTypeName);
                    }
                }

                string? stagedRuntimeConfigPath = null;
                if (!isLibrary)
                {
                    stagedRuntimeConfigPath = StageText(
                        fileSystem,
                        GetRuntimeConfigContents(),
                        runtimeConfigPath);
                    stagedPaths.Add(stagedRuntimeConfigPath);
                }

                var stagedPePath = StageStream(fileSystem, peStream, dllPath);
                stagedPaths.Add(stagedPePath);

                string StagePeWithoutSymbols()
                {
                    result = EmitPeWithoutSymbols(
                        peStream,
                        runtimeAssemblyPath,
                        assemblyName,
                        diagnostics,
                        dllPath,
                        referenceAssemblyPaths,
                        isLibrary,
                        libraryTypeName);
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
        string runtimeAssemblyPath,
        string assemblyName,
        ImmutableArray<Diagnostic> diagnostics,
        string dllPath,
        IEnumerable<string>? referenceAssemblyPaths,
        bool isLibrary,
        string? libraryTypeName)
    {
        peStream.SetLength(0);
        peStream.Position = 0;
        return EmitCore(
            peStream,
            null,
            runtimeAssemblyPath,
            assemblyName,
            diagnostics,
            dllPath,
            pdbPath: null,
            pdbFileName: null,
            referenceAssemblyPaths: referenceAssemblyPaths,
            isLibrary: isLibrary,
            libraryTypeName: libraryTypeName);
    }

    /// <summary>
    /// Emits the compiled assembly to caller-provided streams without creating files.
    /// </summary>
    /// <param name="peStream">A writable stream that receives the portable executable.</param>
    /// <param name="pdbStream">
    /// An optional writable stream that receives portable PDB symbols.
    /// </param>
    /// <param name="cancellationToken">A token checked at compilation and emission boundaries.</param>
    /// <returns>The result of the emission.</returns>
    /// <remarks>
    /// The caller owns both streams. Their positions are advanced but they are not closed.
    /// Runtime references are resolved from the <c>Lolcode.Runtime</c> assembly already
    /// referenced by this compiler, including when that assembly has no file location.
    /// Cancellation does not interrupt parser or binder internals,
    /// but is checked before and after binding and throughout code emission.
    /// </remarks>
    public EmitResult Emit(
        Stream peStream,
        Stream? pdbStream = null,
        CancellationToken cancellationToken = default)
        => Emit(peStream, pdbStream, referenceAssemblyPaths: null, cancellationToken);

    /// <summary>
    /// Emits to caller-provided streams and discovers explicitly opted-in LOLCODE
    /// libraries from resolved reference assembly metadata.
    /// </summary>
    /// <param name="peStream">A writable stream that receives the portable executable.</param>
    /// <param name="pdbStream">An optional writable stream that receives portable PDB symbols.</param>
    /// <param name="referenceAssemblyPaths">
    /// Resolved target reference assembly paths. Supply the complete target reference set
    /// when referenced assemblies contain LOLCODE libraries.
    /// </param>
    /// <param name="cancellationToken">A token checked at compilation and emission boundaries.</param>
    /// <returns>The result of the emission.</returns>
    public EmitResult Emit(
        Stream peStream,
        Stream? pdbStream,
        IEnumerable<string>? referenceAssemblyPaths,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateOutputStream(peStream, nameof(peStream));
        if (pdbStream != null)
            ValidateOutputStream(pdbStream, nameof(pdbStream));
        if (ReferenceEquals(peStream, pdbStream))
            throw new ArgumentException("PE and PDB output streams must be different.", nameof(pdbStream));

        var diagnostics = GetDiagnostics(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            return new EmitResult(false, diagnostics, null);

        return EmitCore(
            peStream,
            pdbStream,
            runtimeAssemblyPath: null,
            _inMemoryAssemblyName,
            diagnostics,
            outputPath: null,
            pdbPath: null,
            pdbFileName: pdbStream == null ? null : $"{_inMemoryAssemblyName}.pdb",
            cancellationToken: cancellationToken,
            referenceAssemblyPaths: referenceAssemblyPaths);
    }

    private EmitResult EmitCore(
        Stream peStream,
        Stream? pdbStream,
        string? runtimeAssemblyPath,
        string assemblyName,
        ImmutableArray<Diagnostic> diagnostics,
        string? outputPath,
        string? pdbPath,
        string? pdbFileName,
        bool toleratePdbFailure = false,
        CancellationToken cancellationToken = default,
        IEnumerable<string>? referenceAssemblyPaths = null,
        bool isLibrary = false,
        string? libraryTypeName = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var bindingResult = EnsureBound();
        cancellationToken.ThrowIfCancellationRequested();
        LibraryDiscoveryResult libraryDiscovery = LibraryDiscovery.Discover(
            referenceAssemblyPaths,
            runtimeAssemblyPath);
        ImmutableArray<Diagnostic> emitDiagnostics = diagnostics.AddRange(libraryDiscovery.Diagnostics);
        if (libraryDiscovery.Diagnostics.Any(diagnostic =>
            diagnostic.Severity == DiagnosticSeverity.Error))
        {
            return new EmitResult(false, emitDiagnostics, null);
        }
        var generator = new CodeGenerator(
            bindingResult.BoundTree,
            assemblyName,
            runtimeAssemblyPath,
            referenceAssemblyPaths,
            SyntaxTrees,
            bindingResult.SyntaxTrees,
            isLibrary: isLibrary,
            libraryTypeName: libraryTypeName,
            libraryDefinitions: libraryDiscovery.Definitions);
        var pdbEmitted = false;
        if (toleratePdbFailure && pdbStream != null && pdbFileName != null)
            pdbEmitted = generator.EmitWithOptionalPdb(peStream, pdbStream, pdbFileName, cancellationToken);
        else
        {
            generator.Emit(peStream, pdbStream, pdbFileName, cancellationToken);
            pdbEmitted = pdbStream != null;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return new EmitResult(true, emitDiagnostics, outputPath, pdbEmitted ? pdbPath : null);
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
        string? runtimeConfigPath,
        string stagedPePath,
        string? stagedPdbPath,
        string? stagedRuntimeConfigPath,
        Func<string> stagePeWithoutSymbols)
    {
        var primaryTargetPaths = runtimeConfigPath is null
            ? new[] { pdbPath, dllPath }
            : new[] { runtimeConfigPath, pdbPath, dllPath };
        var targetPaths = primaryTargetPaths;
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

            if (stagedRuntimeConfigPath is not null && runtimeConfigPath is not null)
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

    private BindingResult EnsureBound()
    {
        lock (_bindingLock)
        {
            if (_bindingResult is not null)
                return _bindingResult;

            var binder = new Binder(
                SyntaxTrees.IsEmpty ? Text.SourceText.From("") : SyntaxTrees[0].Text);
            var binding = binder.BindCompilationUnits(SyntaxTrees);

            // Lower the bound tree (simplify for code generation)
            _bindingResult = new BindingResult(
                Lowerer.Lower(binding.BoundTree),
                binding.Diagnostics,
                binding.SyntaxTrees);
            return _bindingResult;
        }
    }

    private sealed record BindingResult(
        BoundBlockStatement BoundTree,
        ImmutableArray<Diagnostic> Diagnostics,
        ImmutableDictionary<SyntaxNode, SyntaxTree> SyntaxTrees);
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
