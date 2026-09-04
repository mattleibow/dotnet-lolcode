using System.Collections.Immutable;

namespace Lolcode.Web.Execution;

internal sealed record CodeRunRequest(string Source, string StandardInput);

internal sealed record CodeRunResult(
    bool Success,
    bool Executed,
    string Output,
    TimeSpan Duration,
    ImmutableArray<CodeDiagnostic> Diagnostics);

internal sealed record CodeDiagnostic(
    string Id,
    CodeDiagnosticSeverity Severity,
    string Message,
    int? StartLine = null,
    int? StartColumn = null,
    int? EndLine = null,
    int? EndColumn = null);

internal enum CodeDiagnosticSeverity
{
    Info,
    Warning,
    Error,
}
