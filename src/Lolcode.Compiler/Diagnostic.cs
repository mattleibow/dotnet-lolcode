using Lolcode.Compiler.Text;

namespace Lolcode.Compiler;

/// <summary>
/// Represents a compiler diagnostic (error, warning, or info).
/// </summary>
/// <param name="Id">The diagnostic ID (e.g., LOL0001).</param>
/// <param name="Location">The source location of the diagnostic.</param>
/// <param name="Message">The human-readable diagnostic message.</param>
/// <param name="Severity">The severity of the diagnostic.</param>
public sealed record Diagnostic(
    string Id,
    TextLocation Location,
    string Message,
    DiagnosticSeverity Severity = DiagnosticSeverity.Error)
{
    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Location}: {Severity.ToString().ToLowerInvariant()} {Id}: {Message}";
    }
}
