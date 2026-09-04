using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Lolcode.CodeAnalysis.Syntax;
using Lolcode.Runtime;

namespace Lolcode.CodeAnalysis.Scripting;

/// <summary>
/// Provides a scripting-style facade for compiling and running LOLCODE in memory.
/// </summary>
/// <remarks>
/// Compilation and emission remain available separately through
/// <see cref="LolcodeCompilation"/>. This facade is the execution host, analogous
/// to the role of Roslyn's language scripting APIs rather than <c>Compilation</c>.
/// Browser WebAssembly execution uses non-collectible assembly loading, so generated
/// assemblies remain loaded until the application is reloaded.
/// </remarks>
public static class LolcodeScript
{
    /// <summary>
    /// Parses, compiles, and runs LOLCODE entirely in memory.
    /// </summary>
    /// <param name="code">The LOLCODE source to run.</param>
    /// <param name="standardInput">
    /// Text supplied to <c>GIMMEH</c>. A <see langword="null"/> value supplies end-of-input.
    /// </param>
    /// <param name="filePath">
    /// An optional source path used only for diagnostics and portable PDB sequence points.
    /// </param>
    /// <returns>Structured diagnostics, output, return value, and runtime failure state.</returns>
    public static LolcodeScriptResult Run(
        string code,
        string? standardInput = null,
        string? filePath = null)
    {
        ArgumentNullException.ThrowIfNull(code);

        var syntaxTree = SyntaxTree.ParseText(code, filePath ?? "submission.lol");
        return Run(LolcodeCompilation.Create(syntaxTree), standardInput);
    }

    /// <summary>
    /// Emits and runs an existing compilation entirely in memory.
    /// </summary>
    /// <param name="compilation">The compilation to run.</param>
    /// <param name="standardInput">
    /// Text supplied to <c>GIMMEH</c>. A <see langword="null"/> value supplies end-of-input.
    /// </param>
    /// <returns>Structured diagnostics, output, return value, and runtime failure state.</returns>
    /// <remarks>
    /// Each call emits a unique assembly identity. CoreCLR loads it into a collectible
    /// <see cref="AssemblyLoadContext"/> and requests unloading before this method returns.
    /// Browser WebAssembly loads it with <see cref="Assembly.Load(byte[], byte[])"/> because
    /// Mono WebAssembly does not expose generated types as collectible; browser-loaded
    /// assemblies therefore remain for the lifetime of the application.
    /// </remarks>
    public static LolcodeScriptResult Run(
        LolcodeCompilation compilation,
        string? standardInput = null)
        => Run(compilation, standardInput, maximumOutputLength: null);

    /// <summary>
    /// Emits and runs an existing compilation entirely in memory with bounded captured output.
    /// </summary>
    /// <param name="compilation">The compilation to run.</param>
    /// <param name="standardInput">
    /// Text supplied to <c>GIMMEH</c>. A <see langword="null"/> value supplies end-of-input.
    /// </param>
    /// <param name="maximumOutputLength">
    /// The maximum number of output characters to retain, or <see langword="null"/> for no limit.
    /// </param>
    /// <returns>Structured diagnostics, output, return value, and runtime failure state.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="maximumOutputLength"/> is negative.
    /// </exception>
    public static LolcodeScriptResult Run(
        LolcodeCompilation compilation,
        string? standardInput,
        int? maximumOutputLength)
    {
        if (maximumOutputLength is < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumOutputLength),
                maximumOutputLength,
                "The maximum output length cannot be negative.");
        }

        return RunCore(
            compilation,
            standardInput,
            useNonCollectibleAssemblyLoad: OperatingSystem.IsBrowser(),
            maximumOutputLength);
    }

    internal static LolcodeScriptResult RunCore(
        LolcodeCompilation compilation,
        string? standardInput,
        bool useNonCollectibleAssemblyLoad,
        int? maximumOutputLength = null)
    {
        ArgumentNullException.ThrowIfNull(compilation);

        using var peStream = new MemoryStream();
        using var pdbStream = new MemoryStream();
        var emitResult = compilation.Emit(peStream, pdbStream);
        if (!emitResult.Success)
        {
            return new LolcodeScriptResult(
                executed: false,
                emitResult.Diagnostics,
                output: string.Empty,
                returnValue: null,
                exception: null);
        }

        peStream.Position = 0;
        pdbStream.Position = 0;

        using var input = new StringReader(standardInput ?? string.Empty);
        using TextWriter output = maximumOutputLength is { } limit
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

            var entryPoint = assembly.EntryPoint
                ?? throw new InvalidOperationException("Emitted LOLCODE assembly has no entry point.");

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

        return new LolcodeScriptResult(
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
/// Describes the result of an in-memory LOLCODE execution.
/// </summary>
public sealed class LolcodeScriptResult
{
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
    /// Gets the entry point return value. Current LOLCODE programs have a <see langword="void"/>
    /// entry point, so this is normally <see langword="null"/>.
    /// </summary>
    public object? ReturnValue { get; }

    /// <summary>
    /// Gets the exception thrown by the LOLCODE program, or <see langword="null"/> when execution
    /// completed normally or was prevented by compilation errors.
    /// </summary>
    /// <remarks>
    /// Exceptions thrown by generated code are unwrapped from
    /// <see cref="TargetInvocationException"/>.
    /// </remarks>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets whether captured output exceeded the requested maximum length.
    /// </summary>
    public bool OutputTruncated { get; }

    internal LolcodeScriptResult(
        bool executed,
        ImmutableArray<Diagnostic> diagnostics,
        string output,
        object? returnValue,
        Exception? exception,
        bool outputTruncated = false)
    {
        Executed = executed;
        Diagnostics = diagnostics;
        Output = output;
        ReturnValue = returnValue;
        Exception = exception;
        OutputTruncated = outputTruncated;
    }
}
