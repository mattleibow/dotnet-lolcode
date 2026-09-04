using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace Lolcode.Web.Execution;

internal sealed class CSharpCodeRunner(HttpClient httpClient) : ICodeRunner
{
    private const int MaxSourceLength = 100_000;
    private const int MaxInputLength = 32_000;
    private const int MaxOutputLength = 128_000;
    private const string SourceFileName = "Playground.cs";

    private static readonly string[] ReferenceAssemblyNames =
    [
        "System.Collections.Concurrent.dll",
        "System.Collections.dll",
        "System.Console.dll",
        "System.Diagnostics.StackTrace.dll",
        "System.Linq.dll",
        "System.Linq.Expressions.dll",
        "System.Memory.dll",
        "System.Runtime.dll",
        "System.Runtime.Extensions.dll",
        "System.Text.Encoding.dll",
        "System.Text.Json.dll",
        "System.Text.RegularExpressions.dll",
        "System.Threading.dll",
        "netstandard.dll",
    ];

    private Task<ImmutableArray<MetadataReference>>? _referenceLoadTask;

    public string LanguageName => "C# 14";

    public async Task<CodeRunResult> RunAsync(
        CodeRunRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Source.Length > MaxSourceLength)
        {
            return ValidationFailure($"Source is limited to {MaxSourceLength:N0} characters.");
        }

        if (request.StandardInput.Length > MaxInputLength)
        {
            return ValidationFailure($"Standard input is limited to {MaxInputLength:N0} characters.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var stopwatch = Stopwatch.StartNew();
        var references = await GetReferencesAsync();
        cancellationToken.ThrowIfCancellationRequested();

        var syntaxTree = CSharpSyntaxTree.ParseText(
            SourceText.From(request.Source, Encoding.UTF8),
            new CSharpParseOptions(LanguageVersion.CSharp14),
            SourceFileName,
            cancellationToken);
        var inputTree = CSharpSyntaxTree.ParseText(
            SourceText.From(CreateInputHelper(request.StandardInput), Encoding.UTF8),
            new CSharpParseOptions(LanguageVersion.CSharp14),
            "FiddleInput.g.cs",
            cancellationToken);
        var compilation = CSharpCompilation.Create(
            $"LolcodeWeb_{Guid.NewGuid():N}",
            [syntaxTree, inputTree],
            references,
            new CSharpCompilationOptions(
                OutputKind.ConsoleApplication,
                optimizationLevel: OptimizationLevel.Debug,
                warningLevel: 9999));

        using var peStream = new MemoryStream();
        using var pdbStream = new MemoryStream();
        var emitResult = compilation.Emit(
            peStream,
            pdbStream,
            options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb),
            cancellationToken: cancellationToken);
        var diagnostics = emitResult.Diagnostics
            .Where(diagnostic => diagnostic.Severity is
                DiagnosticSeverity.Warning or DiagnosticSeverity.Error)
            .Select(ToCodeDiagnostic)
            .ToImmutableArray();

        if (!emitResult.Success)
        {
            stopwatch.Stop();
            return new CodeRunResult(false, string.Empty, null, stopwatch.Elapsed, diagnostics);
        }

        peStream.Position = 0;
        pdbStream.Position = 0;
        var peImage = peStream.ToArray();
        var pdbImage = pdbStream.ToArray();
        var assembly = Assembly.Load(peImage, pdbImage);

        var output = new LimitedTextWriter(MaxOutputLength);
        var synchronizedOutput = TextWriter.Synchronized(output);
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            Console.SetOut(synchronizedOutput);
            Console.SetError(synchronizedOutput);

            var exitCode = await InvokeEntryPointAsync(assembly, cancellationToken);
            stopwatch.Stop();
            return new CodeRunResult(
                true,
                output.GetContent(),
                exitCode,
                stopwatch.Elapsed,
                diagnostics);
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            var actualException = Unwrap(exception);
            var runtimeDiagnostic = CreateRuntimeDiagnostic(
                actualException,
                assembly,
                pdbImage);

            return new CodeRunResult(
                false,
                output.GetContent(),
                null,
                stopwatch.Elapsed,
                diagnostics.Add(runtimeDiagnostic));
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private Task<ImmutableArray<MetadataReference>> GetReferencesAsync() =>
        _referenceLoadTask ??= LoadReferencesAsync();

    private async Task<ImmutableArray<MetadataReference>> LoadReferencesAsync()
    {
        var references = await Task.WhenAll(ReferenceAssemblyNames.Select(LoadReferenceAsync));
        return references.ToImmutableArray();
    }

    private async Task<MetadataReference> LoadReferenceAsync(string assemblyName)
    {
        var bytes = await httpClient.GetByteArrayAsync(
            $"framework-references/{Path.GetFileNameWithoutExtension(assemblyName)}.bin");
        return MetadataReference.CreateFromImage(ImmutableArray.CreateRange(bytes));
    }

    private static async Task<int> InvokeEntryPointAsync(
        Assembly assembly,
        CancellationToken cancellationToken)
    {
        var entryPoint = assembly.EntryPoint
            ?? throw new InvalidOperationException("The compiled program has no entry point.");
        var parameters = entryPoint.GetParameters().Length == 0
            ? null
            : new object?[] { Array.Empty<string>() };
        var returnValue = entryPoint.Invoke(null, parameters);

        return returnValue switch
        {
            Task<int> exitCodeTask => await exitCodeTask.WaitAsync(cancellationToken),
            Task task => await CompleteTaskAsync(task, cancellationToken),
            int exitCode => exitCode,
            _ => 0,
        };
    }

    private static async Task<int> CompleteTaskAsync(
        Task task,
        CancellationToken cancellationToken)
    {
        await task.WaitAsync(cancellationToken);
        return 0;
    }

    private static CodeDiagnostic ToCodeDiagnostic(Diagnostic diagnostic)
    {
        var severity = diagnostic.Severity switch
        {
            DiagnosticSeverity.Error => CodeDiagnosticSeverity.Error,
            DiagnosticSeverity.Warning => CodeDiagnosticSeverity.Warning,
            _ => CodeDiagnosticSeverity.Info,
        };

        if (!diagnostic.Location.IsInSource)
        {
            return new CodeDiagnostic(diagnostic.Id, severity, diagnostic.GetMessage());
        }

        var span = diagnostic.Location.GetLineSpan().Span;
        return new CodeDiagnostic(
            diagnostic.Id,
            severity,
            diagnostic.GetMessage(),
            span.Start.Line + 1,
            span.Start.Character + 1,
            span.End.Line + 1,
            span.End.Character + 1);
    }

    private static CodeDiagnostic CreateRuntimeDiagnostic(
        Exception exception,
        Assembly assembly,
        byte[] pdbImage)
    {
        var sourceFrame = new StackTrace(exception, true)
            .GetFrames()
            .FirstOrDefault(frame =>
                string.Equals(
                    Path.GetFileName(frame.GetFileName()),
                    SourceFileName,
                    StringComparison.OrdinalIgnoreCase)
                && frame.GetFileLineNumber() > 0);
        var sourceLocation = sourceFrame is not null
            ? (sourceFrame.GetFileLineNumber(), sourceFrame.GetFileColumnNumber())
            : FindPortablePdbLocation(exception, assembly, pdbImage);

        return new CodeDiagnostic(
            "RUNTIME",
            CodeDiagnosticSeverity.Error,
            $"{exception.GetType().Name}: {exception.Message}",
            sourceLocation.Item1 > 0 ? sourceLocation.Item1 : null,
            sourceLocation.Item2 > 0 ? sourceLocation.Item2 : null);
    }

    private static (int Line, int Column) FindPortablePdbLocation(
        Exception exception,
        Assembly assembly,
        byte[] pdbImage)
    {
        using var pdbStream = new MemoryStream(pdbImage, writable: false);
        using var provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
        var reader = provider.GetMetadataReader();

        foreach (var frame in new StackTrace(exception, false).GetFrames())
        {
            var method = frame.GetMethod();
            var ilOffset = frame.GetILOffset();
            if (method?.Module.Assembly != assembly || ilOffset < 0)
            {
                continue;
            }

            var rowNumber = method.MetadataToken & 0x00FFFFFF;
            if (rowNumber <= 0 || rowNumber > reader.MethodDebugInformation.Count)
            {
                continue;
            }

            var debugInformation = reader.GetMethodDebugInformation(
                MetadataTokens.MethodDebugInformationHandle(rowNumber));
            SequencePoint? closestPoint = null;

            foreach (var sequencePoint in debugInformation.GetSequencePoints())
            {
                if (!sequencePoint.IsHidden && sequencePoint.Offset <= ilOffset)
                {
                    closestPoint = sequencePoint;
                }
            }

            if (closestPoint is { } location)
            {
                return (location.StartLine, location.StartColumn);
            }
        }

        return (0, 0);
    }

    private static Exception Unwrap(Exception exception)
    {
        while (exception is TargetInvocationException or AggregateException
               && exception.InnerException is not null)
        {
            exception = exception.InnerException;
        }

        return exception;
    }

    private static string CreateInputHelper(string standardInput)
    {
        var encodedInput = Convert.ToBase64String(Encoding.UTF8.GetBytes(standardInput));
        return $$"""
            #nullable enable

            global using System;
            global using System.Collections.Generic;
            global using System.Linq;
            global using System.Threading.Tasks;

            using System.IO;
            using System.Text;

            internal static class FiddleInput
            {
                private static readonly StringReader Reader = new(
                    Encoding.UTF8.GetString(Convert.FromBase64String("{{encodedInput}}")));

                internal static string? ReadLine() => Reader.ReadLine();
            }
            """;
    }

    private static CodeRunResult ValidationFailure(string message) =>
        new(
            false,
            string.Empty,
            null,
            TimeSpan.Zero,
            [new CodeDiagnostic("INPUT", CodeDiagnosticSeverity.Error, message)]);

    private sealed class LimitedTextWriter(int maximumLength) : TextWriter
    {
        private readonly StringBuilder _content = new();
        private bool _wasTruncated;

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            if (_content.Length < maximumLength)
            {
                _content.Append(value);
            }
            else
            {
                _wasTruncated = true;
            }
        }

        public override void Write(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            var available = maximumLength - _content.Length;
            if (available > 0)
            {
                _content.Append(value.AsSpan(0, Math.Min(value.Length, available)));
            }

            _wasTruncated |= value.Length > available;
        }

        public string GetContent()
        {
            if (_wasTruncated)
            {
                _content.AppendLine();
                _content.Append("[output truncated]");
            }

            return _content.ToString();
        }
    }
}
