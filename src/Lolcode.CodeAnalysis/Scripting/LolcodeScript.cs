using System.Collections.Immutable;
using System.Globalization;
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
    /// <returns>The final script state, including diagnostics, captured output, return value, and runtime failure state.</returns>
    public static LolcodeScriptState Run(
        string code,
        LolcodeScriptOptions? options = null,
        LolcodeScriptExecutionOptions? executionOptions = null)
        => Create(code, options).Run(executionOptions);

    /// <summary>
    /// Gets the compilation that represents the syntax and semantics of this script.
    /// </summary>
    /// <returns>The reusable LOLCODE compilation.</returns>
    public LolcodeCompilation GetCompilation() => _compilation;

    /// <summary>
    /// Compiles the script and returns all syntax and semantic diagnostics without executing it.
    /// </summary>
    /// <returns>All diagnostics produced by the compilation.</returns>
    public ImmutableArray<Diagnostic> Compile() => _compilation.GetDiagnostics();

    /// <summary>
    /// Emits and runs the script entirely in memory.
    /// </summary>
    /// <param name="options">
    /// Input and output capture options, or <see langword="null"/> to use <see cref="LolcodeScriptExecutionOptions.Default"/>.
    /// </param>
    /// <returns>The final script state, including diagnostics, captured output, return value, and runtime failure state.</returns>
    /// <remarks>
    /// Each call emits a unique assembly identity. CoreCLR loads it into a collectible <see cref="AssemblyLoadContext"/> and requests unloading
    /// before this method returns. Browser WebAssembly uses <see cref="Assembly.Load(byte[], byte[])"/> because generated Mono types are not
    /// collectible; browser-loaded assemblies therefore remain for the lifetime of the application.
    /// </remarks>
    public LolcodeScriptState Run(LolcodeScriptExecutionOptions? options = null)
        => RunCore(options, useNonCollectibleAssemblyLoad: OperatingSystem.IsBrowser());

    internal LolcodeScriptState RunCore(LolcodeScriptExecutionOptions? options, bool useNonCollectibleAssemblyLoad)
    {
        options ??= LolcodeScriptExecutionOptions.Default;

        using var peStream = new MemoryStream();
        using var pdbStream = new MemoryStream();
        var emitResult = _compilation.Emit(peStream, pdbStream);
        if (!emitResult.Success)
        {
            return new LolcodeScriptState(
                this,
                executed: false,
                emitResult.Diagnostics,
                output: string.Empty,
                returnValue: null,
                exception: null,
                outputTruncated: false);
        }

        peStream.Position = 0;
        pdbStream.Position = 0;

        using var input = new StringReader(options.StandardInput ?? string.Empty);
        using TextWriter output = options.MaximumOutputLength is { } limit
            ? new BoundedStringWriter(limit)
            : new StringWriter(CultureInfo.InvariantCulture);
        ScriptAssemblyLoadContext? loadContext = null;
        var executed = false;
        object? returnValue = null;
        Exception? runtimeException = null;

        try
        {
            Assembly assembly;
            if (useNonCollectibleAssemblyLoad)
            {
                assembly = Assembly.Load(peStream.ToArray(), pdbStream.ToArray());
            }
            else
            {
                loadContext = new ScriptAssemblyLoadContext();
                assembly = loadContext.LoadFromStream(peStream, pdbStream);
            }

            var entryPoint = assembly.EntryPoint ?? throw new InvalidOperationException("Emitted LOLCODE assembly has no entry point.");

            using var ioScope = LolRuntime.PushIo(input, output);
            executed = true;

            try
            {
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

        return new LolcodeScriptState(
            this,
            executed,
            emitResult.Diagnostics,
            output.ToString() ?? string.Empty,
            returnValue,
            runtimeException,
            output is BoundedStringWriter { IsTruncated: true });
    }

    private sealed class BoundedStringWriter(int maximumLength)
        : TextWriter
    {
        private readonly StringBuilder _builder = new(Math.Min(maximumLength, 4_096));

        public bool IsTruncated { get; private set; }

        public override Encoding Encoding => Encoding.UTF8;

        public override IFormatProvider FormatProvider => CultureInfo.InvariantCulture;

        public override void Write(char value) => Append(value.ToString());

        public override void Write(char[]? buffer, int index, int count)
        {
            if (buffer is not null)
                Append(buffer.AsSpan(index, count));
        }

        public override void Write(ReadOnlySpan<char> buffer) => Append(buffer);

        public override void Write(string? value)
        {
            if (value is not null)
                Append(value.AsSpan());
        }

        public override string ToString() => _builder.ToString();

        private void Append(ReadOnlySpan<char> value)
        {
            var remainingLength = maximumLength - _builder.Length;
            if (remainingLength <= 0)
            {
                IsTruncated |= !value.IsEmpty;
                return;
            }

            var retainedLength = Math.Min(remainingLength, value.Length);
            _builder.Append(value[..retainedLength]);
            IsTruncated |= retainedLength < value.Length;
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
}

/// <summary>
/// Configures a single execution of a <see cref="LolcodeScript"/>.
/// </summary>
public sealed record LolcodeScriptExecutionOptions
{
    private int? _maximumOutputLength;

    /// <summary>
    /// Gets the default execution options.
    /// </summary>
    public static LolcodeScriptExecutionOptions Default { get; } = new();

    /// <summary>
    /// Gets text supplied to <c>GIMMEH</c>. A <see langword="null"/> value supplies end-of-input.
    /// </summary>
    public string? StandardInput { get; init; }

    /// <summary>
    /// Gets the maximum number of output characters to retain, or <see langword="null"/> for no limit.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is negative.</exception>
    public int? MaximumOutputLength
    {
        get => _maximumOutputLength;
        init
        {
            if (value is < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(MaximumOutputLength),
                    value,
                    "The maximum output length cannot be negative.");
            }

            _maximumOutputLength = value;
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
    /// Gets text written by <c>VISIBLE</c>, including output produced before a runtime failure.
    /// </summary>
    public string Output { get; }

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
    /// Gets whether captured output exceeded the requested maximum length.
    /// </summary>
    public bool OutputTruncated { get; }

    internal LolcodeScriptState(
        LolcodeScript script,
        bool executed,
        ImmutableArray<Diagnostic> diagnostics,
        string output,
        object? returnValue,
        Exception? exception,
        bool outputTruncated)
    {
        Script = script;
        Executed = executed;
        Diagnostics = diagnostics;
        Output = output;
        ReturnValue = returnValue;
        Exception = exception;
        OutputTruncated = outputTruncated;
    }
}
