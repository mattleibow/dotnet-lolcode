using Xunit;

namespace Lolcode.EndToEnd.Tests;

/// <summary>
/// Skips an lci-dependent theory locally while making it mandatory in CI when
/// <c>LCI_PATH</c> identifies the pinned executable.
/// </summary>
public sealed class LciTheoryAttribute : TheoryAttribute
{
    /// <summary>Initializes the conditional lci theory attribute.</summary>
    public LciTheoryAttribute()
    {
        string? path = Environment.GetEnvironmentVariable("LCI_PATH");
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            Skip = "Pinned lci is not configured. Set LCI_PATH to the pinned lci executable; CI sets it.";
        }
    }
}
