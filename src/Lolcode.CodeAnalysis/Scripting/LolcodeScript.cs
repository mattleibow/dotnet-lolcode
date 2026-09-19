using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Lolcode.CodeAnalysis.Syntax;
using Lolcode.Runtime;

namespace Lolcode.CodeAnalysis.Scripting;

/// <summary>
/// Represents a reusable LOLCODE script that can be compiled, inspected, and run entirely in memory.
/// </summary>
/// <remarks>
/// Compilation and emission remain separate through <see cref="LolcodeCompilation"/>. Browser WebAssembly execution uses non-collectible
/// assembly loading, so generated assemblies remain loaded until the application is reloaded.
/// </remarks>
public sealed class LolcodeScript
{
    private readonly LolcodeCompilation _compilation;
    private readonly object _emissionLock = new();
    private readonly object _nonCollectibleLoadLock = new();
    private CachedEmission? _cachedEmission;
    private MethodInfo? _nonCollectibleEntryPoint;
    private int _nonCollectibleLoadCount;

    private LolcodeScript(LolcodeCompilation compilation, LolcodeScriptOptions options)
    {
        _compilation = compilation;
        Options = options;
    }

    /// <summary>
    /// Gets the options used to create this script.
    /// </summary>
    public LolcodeScriptOptions Options { get; }

    /// <summary>
    /// Creates a reusable LOLCODE script.
    /// </summary>
    /// <param name="code">The LOLCODE source text.</param>
    /// <param name="options">Script creation options, or <see langword="null"/> to use <see cref="LolcodeScriptOptions.Default"/>.</param>
    /// <returns>A script that can be compiled, inspected, and run.</returns>
    public static LolcodeScript Create(string code, LolcodeScriptOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(code);
        options ??= LolcodeScriptOptions.Default;
        ArgumentNullException.ThrowIfNull(options.FilePath);

        var syntaxTree = SyntaxTree.ParseText(code, options.FilePath);
        return new LolcodeScript(LolcodeCompilation.Create(syntaxTree), options);
    }

    /// <summary>
    /// Creates and runs a LOLCODE script entirely in memory.
    /// </summary>
    /// <param name="code">The LOLCODE source text.</param>
    /// <param name="options">Script creation options, or <see langword="null"/> to use <see cref="LolcodeScriptOptions.Default"/>.</param>
    /// <param name="executionOptions">
    /// Input and output capture options, or <see langword="null"/> to use <see cref="LolcodeScriptExecutionOptions.Default"/>.
    /// </param>
    /// <param name="cancellationToken">A token checked before compilation, emission, loading, and invocation.</param>
    /// <returns>The final script state, including diagnostics, captured output, return value, and runtime failure state.</returns>
    public static LolcodeScriptState Run(
        string code,
        LolcodeScriptOptions? options = null,
        LolcodeScriptExecutionOptions? executionOptions = null,
        CancellationToken cancellationToken = default)
        => Create(code, options).Run(executionOptions, cancellationToken);

    /// <summary>
    /// Gets the compilation that represents the syntax and semantics of this script.
    /// </summary>
    /// <returns>The reusable LOLCODE compilation.</returns>
    public LolcodeCompilation GetCompilation() => _compilation;

    /// <summary>
    /// Compiles the script and returns all syntax and semantic diagnostics without executing it.
    /// </summary>
    /// <param name="cancellationToken">A token checked before and after the compilation boundary.</param>
    /// <returns>All diagnostics produced by the compilation.</returns>
    public ImmutableArray<Diagnostic> Compile(CancellationToken cancellationToken = default)
    {
        CachedEmission emission = GetOrEmit(cancellationToken);
        return emission.EmitResult.Diagnostics;
    }

    /// <summary>
    /// Emits and runs the script entirely in memory.
    /// </summary>
    /// <param name="options">
    /// Input and output capture options, or <see langword="null"/> to use <see cref="LolcodeScriptExecutionOptions.Default"/>.
    /// </param>
    /// <param name="cancellationToken">A token checked before compilation, emission, loading, and invocation.</param>
    /// <returns>The final script state, including diagnostics, captured output, return value, and runtime failure state.</returns>
    /// <remarks>
    /// Emission is cached per script. CoreCLR loads cached bytes into a new collectible <see cref="AssemblyLoadContext"/> for each run and
    /// requests unloading before this method returns. Browser WebAssembly loads and caches one non-collectible assembly per script because
    /// generated Mono types are not collectible; that assembly remains for the application's lifetime.
    /// Cancellation is checked at compile, emission, load, and immediately-before-invocation boundaries. It does not stop generated code after
    /// invocation begins.
    /// </remarks>
    public LolcodeScriptState Run(
        LolcodeScriptExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
        => RunCore(options, useNonCollectibleAssemblyLoad: OperatingSystem.IsBrowser(), cancellationToken);

    internal int NonCollectibleLoadCount => Volatile.Read(ref _nonCollectibleLoadCount);

    internal bool HasCachedPdb => _cachedEmission?.PdbBytes is { Length: > 0 };

    internal LolcodeScriptState RunCore(
        LolcodeScriptExecutionOptions? options,
        bool useNonCollectibleAssemblyLoad,
        CancellationToken cancellationToken = default)
    {
        options ??= LolcodeScriptExecutionOptions.Default;
        CachedEmission emission = GetOrEmit(cancellationToken);
        if (!emission.EmitResult.Success)
            return CreateUnexecutedState(emission.EmitResult.Diagnostics);

        cancellationToken.ThrowIfCancellationRequested();
        using var input = new StringReader(options.StandardInput ?? string.Empty);
        using var standardOutputStream = new BoundedWritableStream(options.MaximumStandardOutputBytes);
        using var standardErrorStream = new BoundedWritableStream(options.MaximumStandardErrorBytes);
        using var standardOutput = CreateWriter(standardOutputStream);
        using var standardError = CreateWriter(standardErrorStream);
        ScriptAssemblyLoadContext? loadContext = null;
        var executed = false;
        object? returnValue = null;
        Exception? runtimeException = null;

        try
        {
            MethodInfo entryPoint;
            if (useNonCollectibleAssemblyLoad)
            {
                entryPoint = GetOrLoadNonCollectibleEntryPoint(emission, cancellationToken);
            }
            else
            {
                loadContext = new ScriptAssemblyLoadContext();
                using var peStream = new MemoryStream(emission.PeBytes, writable: false);
                using var pdbStream = emission.PdbBytes is null
                    ? null
                    : new MemoryStream(emission.PdbBytes, writable: false);
                cancellationToken.ThrowIfCancellationRequested();
                Assembly assembly = pdbStream is null
                    ? loadContext.LoadFromStream(peStream)
                    : loadContext.LoadFromStream(peStream, pdbStream);
                entryPoint = assembly.EntryPoint ?? throw new InvalidOperationException("Emitted LOLCODE assembly has no entry point.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            using var ioScope = LolRuntime.PushIo(input, standardOutput, standardError);
            executed = true;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                returnValue = entryPoint.Invoke(obj: null, parameters: null);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                runtimeException = ex.InnerException;
            }
        }
        finally
        {
            loadContext?.Unload();
        }

        standardOutput.Flush();
        standardError.Flush();
        byte[] standardOutputBytes = standardOutputStream.ToArray();
        byte[] standardErrorBytes = standardErrorStream.ToArray();
        return new LolcodeScriptState(
            this,
            executed,
            emission.EmitResult.Diagnostics,
            standardOutputBytes,
            standardErrorBytes,
            returnValue,
            runtimeException,
            standardOutputStream.IsTruncated,
            standardErrorStream.IsTruncated);
    }

    private CachedEmission GetOrEmit(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_emissionLock)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_cachedEmission is { } cached)
                return cached;

            using var peStream = new MemoryStream();
            using var pdbStream = Options.EmitDebugInformation ? new MemoryStream() : null;
            EmitResult emitResult = _compilation.Emit(peStream, pdbStream, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return _cachedEmission = new CachedEmission(
                emitResult,
                emitResult.Success ? peStream.ToArray() : [],
                emitResult.Success && pdbStream is not null ? pdbStream.ToArray() : null);
        }
    }

    private MethodInfo GetOrLoadNonCollectibleEntryPoint(
        CachedEmission emission,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_nonCollectibleLoadLock)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_nonCollectibleEntryPoint is { } entryPoint)
                return entryPoint;

            Assembly assembly = emission.PdbBytes is null
                ? Assembly.Load(emission.PeBytes)
                : Assembly.Load(emission.PeBytes, emission.PdbBytes);
            _nonCollectibleEntryPoint = assembly.EntryPoint
                ?? throw new InvalidOperationException("Emitted LOLCODE assembly has no entry point.");
            Interlocked.Increment(ref _nonCollectibleLoadCount);
            return _nonCollectibleEntryPoint;
        }
    }

    private LolcodeScriptState CreateUnexecutedState(ImmutableArray<Diagnostic> diagnostics) =>
        new(
            this,
            executed: false,
            diagnostics,
            [],
            [],
            returnValue: null,
            exception: null,
            standardOutputTruncated: false,
            standardErrorTruncated: false);

    private static StreamWriter CreateWriter(Stream stream) =>
        new(
            stream,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            bufferSize: 1_024,
            leaveOpen: true);

    private sealed record CachedEmission(
        EmitResult EmitResult,
        byte[] PeBytes,
        byte[]? PdbBytes);

    private sealed class BoundedWritableStream(int? maximumBytes) : Stream
    {
        private readonly MemoryStream _stream = new();

        public bool IsTruncated { get; private set; }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _stream.Length;
        public override long Position
        {
            get => _stream.Position;
            set => throw new NotSupportedException();
        }

        public byte[] ToArray() => _stream.ToArray();

        public override void Flush() => _stream.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken) =>
            _stream.FlushAsync(cancellationToken);

        public override int Read(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) =>
            throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            Write(buffer.AsSpan(offset, count));

        public override void WriteByte(byte value)
        {
            if (maximumBytes is { } maximum && _stream.Length >= maximum)
            {
                IsTruncated = true;
                return;
            }

            _stream.WriteByte(value);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            int retainedLength = maximumBytes is { } maximum
                ? Math.Min(Math.Max(0, maximum - checked((int)_stream.Length)), buffer.Length)
                : buffer.Length;
            if (retainedLength > 0)
                _stream.Write(buffer[..retainedLength]);
            IsTruncated |= retainedLength < buffer.Length;
        }

        public override Task WriteAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken) =>
            WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override ValueTask WriteAsync(
            ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Write(buffer.Span);
            return ValueTask.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _stream.Dispose();
            base.Dispose(disposing);
        }
    }

    private sealed class ScriptAssemblyLoadContext()
        : AssemblyLoadContext(isCollectible: true)
    {
        private static readonly Assembly RuntimeAssembly = typeof(LolRuntime).Assembly;
        private static readonly AssemblyName RuntimeAssemblyName = RuntimeAssembly.GetName();

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            return AssemblyName.ReferenceMatchesDefinition(assemblyName, RuntimeAssemblyName)
                ? RuntimeAssembly
                : null;
        }
    }
}

/// <summary>
/// Configures the source represented by a <see cref="LolcodeScript"/>.
/// </summary>
public sealed record LolcodeScriptOptions
{
    /// <summary>
    /// Gets the default script options.
    /// </summary>
    public static LolcodeScriptOptions Default { get; } = new();

    /// <summary>
    /// Gets the source path used for diagnostics and portable PDB sequence points.
    /// </summary>
    public string FilePath { get; init; } = "submission.lol";

    /// <summary>
    /// Gets whether script emission includes portable PDB debug information.
    /// </summary>
    /// <remarks>
    /// The default is <see langword="false"/>, matching Roslyn scripting. Set this to
    /// <see langword="true"/> only when a debugger or symbol-aware host needs a PDB.
    /// </remarks>
    public bool EmitDebugInformation { get; init; }
}

/// <summary>
/// Configures a single execution of a <see cref="LolcodeScript"/>.
/// </summary>
public sealed record LolcodeScriptExecutionOptions
{
    private int? _maximumStandardOutputBytes;
    private int? _maximumStandardErrorBytes;

    /// <summary>
    /// Gets the default execution options.
    /// </summary>
    public static LolcodeScriptExecutionOptions Default { get; } = new();

    /// <summary>
    /// Gets text supplied to <c>GIMMEH</c>. A <see langword="null"/> value supplies end-of-input.
    /// </summary>
    public string? StandardInput { get; init; }

    /// <summary>
    /// Gets the maximum number of standard-output bytes to retain, or <see langword="null"/> for no limit.
    /// </summary>
    /// <remarks>
    /// The limit bounds retained bytes, not program execution. Text is encoded as UTF-8 without a BOM. A byte limit can retain part
    /// of a multi-byte UTF-8 sequence; <see cref="LolcodeScriptState.StandardOutput"/> decodes such data with replacement characters.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is negative.</exception>
    public int? MaximumStandardOutputBytes
    {
        get => _maximumStandardOutputBytes;
        init
        {
            if (value is < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(MaximumStandardOutputBytes),
                    value,
                    "The maximum standard-output byte count cannot be negative.");
            }

            _maximumStandardOutputBytes = value;
        }
    }

    /// <summary>
    /// Gets the maximum number of standard-error bytes to retain, or <see langword="null"/> for no limit.
    /// </summary>
    /// <remarks>
    /// The limit bounds retained bytes, not program execution. Text is encoded as UTF-8 without a BOM. A byte limit can retain part
    /// of a multi-byte UTF-8 sequence; <see cref="LolcodeScriptState.StandardError"/> decodes such data with replacement characters.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is negative.</exception>
    public int? MaximumStandardErrorBytes
    {
        get => _maximumStandardErrorBytes;
        init
        {
            if (value is < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(MaximumStandardErrorBytes),
                    value,
                    "The maximum standard-error byte count cannot be negative.");
            }

            _maximumStandardErrorBytes = value;
        }
    }
}

/// <summary>
/// Describes the final state of an in-memory LOLCODE execution.
/// </summary>
public sealed class LolcodeScriptState
{
    /// <summary>
    /// Gets the script that produced this state.
    /// </summary>
    public LolcodeScript Script { get; }

    /// <summary>
    /// Gets whether compilation and execution completed without errors.
    /// </summary>
    public bool Success => Executed && Exception == null;

    /// <summary>
    /// Gets whether the emitted entry point was invoked.
    /// </summary>
    public bool Executed { get; }

    /// <summary>
    /// Gets all syntax and semantic diagnostics produced by the compilation.
    /// </summary>
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets standard-output text written by <c>VISIBLE</c>, including output produced before a runtime failure.
    /// </summary>
    /// <remarks>
    /// This is UTF-8 decoded from <see cref="StandardOutputBytes"/> with replacement fallback. If byte retention ended in the middle of a
    /// UTF-8 sequence, this value contains the replacement character.
    /// </remarks>
    public string StandardOutput { get; }

    /// <summary>
    /// Gets standard-error text written by <c>INVISIBLE</c> and system commands, including output produced before a runtime failure.
    /// </summary>
    /// <remarks>
    /// This is UTF-8 decoded from <see cref="StandardErrorBytes"/> with replacement fallback. If byte retention ended in the middle of a
    /// UTF-8 sequence, this value contains the replacement character.
    /// </remarks>
    public string StandardError { get; }

    /// <summary>
    /// Gets the exact retained standard-output bytes.
    /// </summary>
    public ImmutableArray<byte> StandardOutputBytes { get; }

    /// <summary>
    /// Gets the exact retained standard-error bytes.
    /// </summary>
    public ImmutableArray<byte> StandardErrorBytes { get; }

    /// <summary>
    /// Gets the entry point return value. Current LOLCODE programs have a <see langword="void"/> entry point, so this is normally
    /// <see langword="null"/>.
    /// </summary>
    public object? ReturnValue { get; }

    /// <summary>
    /// Gets the exception thrown by the LOLCODE program, or <see langword="null"/> when execution completed normally or was prevented by
    /// compilation errors.
    /// </summary>
    /// <remarks>Exceptions thrown by generated code are unwrapped from <see cref="TargetInvocationException"/>.</remarks>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets whether standard output exceeded the requested maximum retained byte count.
    /// </summary>
    public bool StandardOutputTruncated { get; }

    /// <summary>
    /// Gets whether standard error exceeded the requested maximum retained byte count.
    /// </summary>
    public bool StandardErrorTruncated { get; }

    internal LolcodeScriptState(
        LolcodeScript script,
        bool executed,
        ImmutableArray<Diagnostic> diagnostics,
        byte[] standardOutputBytes,
        byte[] standardErrorBytes,
        object? returnValue,
        Exception? exception,
        bool standardOutputTruncated,
        bool standardErrorTruncated)
    {
        Script = script;
        Executed = executed;
        Diagnostics = diagnostics;
        StandardOutputBytes = ImmutableArray.CreateRange(standardOutputBytes);
        StandardErrorBytes = ImmutableArray.CreateRange(standardErrorBytes);
        StandardOutput = Encoding.UTF8.GetString(standardOutputBytes);
        StandardError = Encoding.UTF8.GetString(standardErrorBytes);
        ReturnValue = returnValue;
        Exception = exception;
        StandardOutputTruncated = standardOutputTruncated;
        StandardErrorTruncated = standardErrorTruncated;
    }
}
