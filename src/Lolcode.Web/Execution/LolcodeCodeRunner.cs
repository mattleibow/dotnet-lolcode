using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Lolcode.CodeAnalysis;
using Lolcode.CodeAnalysis.Scripting;
using Lolcode.CodeAnalysis.Syntax;
using CompilerDiagnosticSeverity = Lolcode.CodeAnalysis.DiagnosticSeverity;

namespace Lolcode.Web.Execution;

internal sealed class LolcodeCodeRunner : ICodeRunner
{
    private const string SourceFileName = "Program.lol";

    public string LanguageName => "LOLCODE 1.2";

    public Task<CodeRunResult> RunAsync(
        CodeRunRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Source.Length > CodeRunnerLimits.MaxSourceLength)
        {
            return Task.FromResult(
                ValidationFailure(
                    $"Source is limited to {CodeRunnerLimits.MaxSourceLength:N0} characters."));
        }

        if (request.StandardInput.Length > CodeRunnerLimits.MaxInputLength)
        {
            return Task.FromResult(
                ValidationFailure(
                    $"Program input is limited to {CodeRunnerLimits.MaxInputLength:N0} characters."));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var stopwatch = Stopwatch.StartNew();
        var syntaxTree = SyntaxTree.ParseText(request.Source, SourceFileName);
        var compilation = LolcodeCompilation.Create(syntaxTree);
        var scriptResult = LolcodeScript.Run(compilation, request.StandardInput);
        var diagnostics = scriptResult.Diagnostics
            .Select(ToCodeDiagnostic)
            .ToImmutableArray();

        if (scriptResult.Exception is not null)
        {
            diagnostics = diagnostics.Add(
                CreateRuntimeDiagnostic(scriptResult.Exception, compilation));
        }

        stopwatch.Stop();
        return Task.FromResult(
            new CodeRunResult(
                scriptResult.Success,
                scriptResult.Executed,
                TruncateOutput(scriptResult.Output),
                stopwatch.Elapsed,
                diagnostics));
    }

    private static CodeDiagnostic ToCodeDiagnostic(Diagnostic diagnostic) =>
        new(
            diagnostic.Id,
            diagnostic.Severity switch
            {
                CompilerDiagnosticSeverity.Error => CodeDiagnosticSeverity.Error,
                CompilerDiagnosticSeverity.Warning => CodeDiagnosticSeverity.Warning,
                _ => CodeDiagnosticSeverity.Info,
            },
            diagnostic.Message,
            diagnostic.Location.StartLine + 1,
            diagnostic.Location.StartCharacter + 1,
            diagnostic.Location.EndLine + 1,
            diagnostic.Location.EndCharacter + 1);

    internal static CodeDiagnostic CreateRuntimeDiagnostic(
        Exception exception,
        LolcodeCompilation compilation)
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
            ? (Line: sourceFrame.GetFileLineNumber(), Column: sourceFrame.GetFileColumnNumber())
            : FindPortablePdbLocation(exception, compilation);

        return new CodeDiagnostic(
            "RUNTIME",
            CodeDiagnosticSeverity.Error,
            $"{exception.GetType().Name}: {exception.Message}",
            sourceLocation.Line > 0 ? sourceLocation.Line : null,
            sourceLocation.Column > 0 ? sourceLocation.Column : null);
    }

    internal static (int Line, int Column) FindPortablePdbLocation(
        Exception exception,
        LolcodeCompilation compilation)
    {
        using var peStream = new MemoryStream();
        using var pdbStream = new MemoryStream();
        var emitResult = compilation.Emit(peStream, pdbStream);
        if (!emitResult.Success)
        {
            return (0, 0);
        }

        pdbStream.Position = 0;
        using var provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
        var reader = provider.GetMetadataReader();

        foreach (var frame in new StackTrace(exception, false).GetFrames())
        {
            var method = frame.GetMethod();
            var ilOffset = frame.GetILOffset();
            if (method?.DeclaringType?.FullName != "Program" || ilOffset < 0)
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

    internal static string TruncateOutput(string output)
    {
        if (output.Length <= CodeRunnerLimits.MaxOutputLength)
        {
            return output;
        }

        return string.Concat(
            output.AsSpan(0, CodeRunnerLimits.MaxOutputLength),
            Environment.NewLine,
            "[output truncated]");
    }

    private static CodeRunResult ValidationFailure(string message) =>
        new(
            false,
            false,
            string.Empty,
            TimeSpan.Zero,
            [new CodeDiagnostic("INPUT", CodeDiagnosticSeverity.Error, message)]);
}
