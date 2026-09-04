using System.Text.RegularExpressions;

namespace Lolcode.EndToEnd.Tests;

internal static partial class LciConformanceCorpus
{
    private static readonly Lazy<IReadOnlyList<LciTestRegistration>> RegistrationsValue =
        new(LoadRegistrations);

    internal static IReadOnlyList<LciTestRegistration> Registrations => RegistrationsValue.Value;

    private static string CorpusRoot =>
        Path.Combine(AppContext.BaseDirectory, "Conformance", "lci");

    private static IReadOnlyList<LciTestRegistration> LoadRegistrations()
    {
        string testRoot = Path.Combine(CorpusRoot, "upstream", "test");
        var registrations = new List<LciTestRegistration>();

        foreach (string cmakePath in Directory.EnumerateFiles(
                     testRoot, "CMakeLists.txt", SearchOption.AllDirectories))
        {
            string cmake = File.ReadAllText(cmakePath);
            MatchCollection matches = RegistrationRegex().Matches(cmake);
            if (matches.Count == 0)
                continue;

            foreach (Match match in matches)
            {
                string directory = Path.GetDirectoryName(cmakePath)!;
                string id = Path.GetRelativePath(testRoot, directory).Replace('\\', '/');
                string[] arguments = WhitespaceRegex()
                    .Split(match.Groups["arguments"].Value.Trim());

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
                            source = ResolveArgumentPath(directory, arguments, ref index);
                            break;
                        case "OUTPUT":
                            expectedOutput = ResolveArgumentPath(directory, arguments, ref index);
                            break;
                        case "INPUT":
                            input = ResolveArgumentPath(directory, arguments, ref index);
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
                registrations.Add(new LciTestRegistration(
                    id,
                    arguments[0],
                    source,
                    expectedOutput,
                    input,
                    expectError ? expectedError : null,
                    expectError,
                    useWorkingDirectory ? directory : null));
            }
        }

        return registrations.OrderBy(test => test.Id, StringComparer.Ordinal).ToArray();
    }

    private static string ResolveArgumentPath(
        string directory,
        IReadOnlyList<string> arguments,
        ref int index)
    {
        index++;
        if (index >= arguments.Count)
            throw new InvalidDataException("ADD_LOL_TEST metadata is missing a path argument.");

        return Path.Combine(directory, arguments[index]);
    }

    [GeneratedRegex(@"ADD_LOL_TEST\s*\((?<arguments>[^)]*)\)")]
    private static partial Regex RegistrationRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}

internal sealed record LciTestRegistration(
    string Id,
    string UpstreamName,
    string SourcePath,
    string? ExpectedOutputPath,
    string? InputPath,
    string? ExpectedErrorPath,
    bool ExpectError,
    string? WorkingDirectoryPath);
