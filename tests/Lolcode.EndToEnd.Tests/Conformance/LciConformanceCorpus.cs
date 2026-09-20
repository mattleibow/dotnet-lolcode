using System.Text.RegularExpressions;

namespace Lolcode.EndToEnd.Tests;

/// <summary>
/// Discovers upstream lci tests from their CMake <c>ADD_LOL_TEST</c> metadata.
/// </summary>
internal static partial class LciConformanceCorpus
{
    private static readonly Lazy<IReadOnlyList<LciTestRegistration>> RegistrationsValue =
        new(LoadRegistrations);

    internal static IReadOnlyList<LciTestRegistration> Registrations => RegistrationsValue.Value;

    private static string CorpusRoot =>
        Path.Combine(AppContext.BaseDirectory, "Conformance", "lci");

    private static IReadOnlyList<LciTestRegistration> LoadRegistrations() =>
        LciRegistrationParser.Discover(
            Path.Combine(CorpusRoot, "upstream", "test"),
            "upstream");

}

internal sealed record LciTestRegistration(
    string Id,
    string UpstreamName,
    string SourcePath,
    string? ExpectedOutputPath,
    string? InputPath,
    string? ExpectedErrorPath,
    string? ExpectedDiagnosticPath,
    bool ExpectError,
    string? WorkingDirectoryPath);

/// <summary>Discovers the repository-owned, portable compatibility fixtures.</summary>
internal static class CompatibilityCorpus
{
    private static readonly Lazy<IReadOnlyList<LciTestRegistration>> RegistrationsValue =
        new(LoadRegistrations);

    internal static IReadOnlyList<LciTestRegistration> Registrations => RegistrationsValue.Value;

    private static IReadOnlyList<LciTestRegistration> LoadRegistrations() =>
        LciRegistrationParser.Discover(
            Path.Combine(AppContext.BaseDirectory, "Compatibility", "Shared"),
            "shared compatibility");
}

/// <summary>Discovers fixtures with a documented pinned-lci semantic or parser divergence.</summary>
internal static class KnownLciDivergenceCompatibilityCorpus
{
    private static readonly Lazy<IReadOnlyList<LciTestRegistration>> RegistrationsValue =
        new(LoadRegistrations);

    internal static IReadOnlyList<LciTestRegistration> Registrations => RegistrationsValue.Value;

    private static IReadOnlyList<LciTestRegistration> LoadRegistrations() =>
        LciRegistrationParser.Discover(
            Path.Combine(AppContext.BaseDirectory, "Compatibility", "KnownLciDivergence"),
            "known-lci-divergence compatibility");
}

/// <summary>
/// Parses the deliberately small, proven <c>ADD_LOL_TEST</c> vocabulary used by lci.
/// The parser is shared by upstream and repository-owned fixture trees so their
/// interpretation cannot drift.
/// </summary>
internal static partial class LciRegistrationParser
{
    internal static IReadOnlyList<LciTestRegistration> Discover(
        string root,
        string corpusName,
        IReadOnlySet<string>? excludedDirectoryNames = null)
    {
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"The {corpusName} corpus was not copied to '{root}'.");

        var registrations = new List<LciTestRegistration>();
        foreach (string cmakePath in Directory.EnumerateFiles(
                     root, "CMakeLists.txt", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(root, cmakePath);
            if (excludedDirectoryNames?.Contains(relativePath.Split(Path.DirectorySeparatorChar)[0]) == true)
                continue;

            string cmake = StripComments(File.ReadAllText(cmakePath));
            foreach (Match match in RegistrationRegex().Matches(cmake))
            {
                string directory = Path.GetDirectoryName(cmakePath)!;
                string[] arguments = WhitespaceRegex()
                    .Split(match.Groups["arguments"].Value.Trim())
                    .Where(argument => argument.Length > 0)
                    .ToArray();
                if (arguments.Length == 0)
                    throw new InvalidDataException($"ADD_LOL_TEST has no test name in {cmakePath}.");

                string source = Path.Combine(directory, "test.lol");
                string? expectedOutput = null;
                string? input = null;
                bool expectError = false;
                bool useWorkingDirectory = false;

                for (int index = 1; index < arguments.Length; index++)
                {
                    switch (arguments[index])
                    {
                        case "LOLCODE":
                            source = ResolveArgumentPath(directory, arguments, ref index, cmakePath);
                            break;
                        case "OUTPUT":
                            expectedOutput = ResolveArgumentPath(directory, arguments, ref index, cmakePath);
                            break;
                        case "INPUT":
                            input = ResolveArgumentPath(directory, arguments, ref index, cmakePath);
                            break;
                        case "ERROR":
                            expectError = true;
                            break;
                        case "CWD":
                            useWorkingDirectory = true;
                            break;
                        default:
                            throw new InvalidDataException(
                                $"Unknown ADD_LOL_TEST argument '{arguments[index]}' in {cmakePath}.");
                    }
                }

                string expectedError = Path.Combine(directory, "test.err");
                string expectedDiagnostic = Path.Combine(directory, "test.diag");
                ValidatePath(source, "LOLCODE source", cmakePath);
                if (expectedOutput is not null)
                    ValidatePath(expectedOutput, "OUTPUT", cmakePath);
                if (input is not null)
                    ValidatePath(input, "INPUT", cmakePath);
                if (expectError && File.Exists(expectedError) && File.Exists(expectedDiagnostic))
                    throw new InvalidDataException(
                        $"ADD_LOL_TEST cannot use both test.err and test.diag in {cmakePath}.");
                registrations.Add(new LciTestRegistration(
                    Path.GetRelativePath(root, directory).Replace('\\', '/'),
                    arguments[0],
                    source,
                    expectedOutput,
                    input,
                    expectError && File.Exists(expectedError) ? expectedError : null,
                    expectError && File.Exists(expectedDiagnostic) ? expectedDiagnostic : null,
                    expectError,
                    useWorkingDirectory ? directory : null));
            }
        }

        return registrations.OrderBy(test => test.Id, StringComparer.Ordinal).ToArray();
    }

    private static string StripComments(string cmake) =>
        string.Join(
            Environment.NewLine,
            cmake.Split(["\r\n", "\n"], StringSplitOptions.None)
                .Select(StripComment));

    private static string StripComment(string line)
    {
        int comment = line.IndexOf('#');
        return comment < 0 ? line : line[..comment];
    }

    private static void ValidatePath(string path, string kind, string cmakePath)
    {
        if (!File.Exists(path))
            throw new InvalidDataException(
                $"ADD_LOL_TEST {kind} path '{path}' does not exist ({cmakePath}).");
    }

    private static string ResolveArgumentPath(
        string directory,
        IReadOnlyList<string> arguments,
        ref int index,
        string cmakePath)
    {
        index++;
        if (index >= arguments.Count)
        {
            throw new InvalidDataException(
                $"ADD_LOL_TEST metadata is missing a path argument in {cmakePath}.");
        }

        return Path.Combine(directory, arguments[index]);
    }

    [GeneratedRegex(
        @"^[ \t]*ADD_LOL_TEST\s*\((?<arguments>[^)]*)\)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline)]
    private static partial Regex RegistrationRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
