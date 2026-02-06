using System.Collections;
using Lolcode.Compiler.Text;

namespace Lolcode.Compiler;

/// <summary>
/// A mutable bag for collecting diagnostics during compilation.
/// </summary>
public sealed class DiagnosticBag : IEnumerable<Diagnostic>
{
    private readonly List<Diagnostic> _diagnostics = [];

    /// <summary>
    /// Gets the number of diagnostics in the bag.
    /// </summary>
    public int Count => _diagnostics.Count;

    /// <summary>
    /// Returns true if the bag contains any errors.
    /// </summary>
    public bool HasErrors => _diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    /// <summary>
    /// Adds all diagnostics from another bag.
    /// </summary>
    public void AddRange(IEnumerable<Diagnostic> diagnostics)
    {
        _diagnostics.AddRange(diagnostics);
    }

    /// <summary>
    /// Reports an error diagnostic.
    /// </summary>
    public void ReportError(string id, TextLocation location, string message)
    {
        _diagnostics.Add(new Diagnostic(id, location, message, DiagnosticSeverity.Error));
    }

    /// <summary>
    /// Reports a warning diagnostic.
    /// </summary>
    public void ReportWarning(string id, TextLocation location, string message)
    {
        _diagnostics.Add(new Diagnostic(id, location, message, DiagnosticSeverity.Warning));
    }

    // --- Lexer Diagnostics ---

    /// <summary>Reports an unexpected character.</summary>
    public void ReportUnexpectedCharacter(TextLocation location, char character)
    {
        ReportError("LOL0001", location, $"Unexpected character '{character}'.");
    }

    /// <summary>Reports an unterminated string literal.</summary>
    public void ReportUnterminatedString(TextLocation location)
    {
        ReportError("LOL0002", location, "Unterminated string literal.");
    }

    /// <summary>Reports an invalid number literal.</summary>
    public void ReportInvalidNumber(TextLocation location, string text)
    {
        ReportError("LOL0003", location, $"Invalid number literal '{text}'.");
    }

    /// <summary>Reports an invalid escape sequence.</summary>
    public void ReportInvalidEscapeSequence(TextLocation location, string sequence)
    {
        ReportError("LOL0004", location, $"Invalid escape sequence '{sequence}'.");
    }

    // --- Parser Diagnostics ---

    /// <summary>Reports an unexpected token.</summary>
    public void ReportUnexpectedToken(TextLocation location, string actual, string expected)
    {
        ReportError("LOL1001", location, $"Unexpected token '{actual}', expected {expected}.");
    }

    /// <summary>Reports a missing expected token.</summary>
    public void ReportExpectedToken(TextLocation location, string expected)
    {
        ReportError("LOL1002", location, $"Expected {expected}.");
    }

    /// <summary>Reports a missing HAI statement.</summary>
    public void ReportMissingHai(TextLocation location)
    {
        ReportError("LOL1003", location, "Program must start with 'HAI'.");
    }

    /// <summary>Reports a missing KTHXBYE statement.</summary>
    public void ReportMissingKthxbye(TextLocation location)
    {
        ReportError("LOL1004", location, "Program must end with 'KTHXBYE'.");
    }

    // --- Binder Diagnostics ---

    /// <summary>Reports an undeclared variable.</summary>
    public void ReportUndeclaredVariable(TextLocation location, string name)
    {
        ReportError("LOL2001", location, $"Variable '{name}' has not been declared.");
    }

    /// <summary>Reports a variable that has already been declared.</summary>
    public void ReportVariableAlreadyDeclared(TextLocation location, string name)
    {
        ReportError("LOL2002", location, $"Variable '{name}' has already been declared.");
    }

    /// <summary>Reports a function that has already been declared.</summary>
    public void ReportFunctionAlreadyDeclared(TextLocation location, string name)
    {
        ReportError("LOL2002", location, $"Function '{name}' has already been declared.");
    }

    /// <summary>Reports an undefined function.</summary>
    public void ReportUndefinedFunction(TextLocation location, string name)
    {
        ReportError("LOL2003", location, $"Function '{name}' is not defined.");
    }

    /// <summary>Reports wrong argument count for a function call.</summary>
    public void ReportWrongArgumentCount(TextLocation location, string name, int expected, int actual)
    {
        ReportError("LOL2004", location, $"Function '{name}' expects {expected} argument(s) but got {actual}.");
    }

    /// <summary>Reports an invalid cast.</summary>
    public void ReportInvalidCast(TextLocation location, string fromType, string toType)
    {
        ReportError("LOL2005", location, $"Cannot cast '{fromType}' to '{toType}'.");
    }

    /// <summary>Reports GTFO used outside of loop, switch, or function.</summary>
    public void ReportInvalidGtfo(TextLocation location)
    {
        ReportError("LOL2006", location, "'GTFO' is not valid in this context.");
    }

    /// <summary>Reports FOUND YR used outside of a function.</summary>
    public void ReportInvalidFoundYr(TextLocation location)
    {
        ReportError("LOL2007", location, "'FOUND YR' is not valid outside a function.");
    }

    /// <summary>Reports duplicate OMG literal in WTF? block.</summary>
    public void ReportDuplicateOmgLiteral(TextLocation location, string value)
    {
        ReportError("LOL2008", location, $"Duplicate OMG case value '{value}'.");
    }

    /// <summary>Reports non-literal in OMG case.</summary>
    public void ReportOmgRequiresLiteral(TextLocation location)
    {
        ReportError("LOL2009", location, "OMG case values must be literals.");
    }

    /// <inheritdoc/>
    public IEnumerator<Diagnostic> GetEnumerator() => _diagnostics.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
