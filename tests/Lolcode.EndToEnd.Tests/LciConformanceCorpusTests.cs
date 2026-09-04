using System.Security.Cryptography;
using System.Text;

namespace Lolcode.EndToEnd.Tests;

public class LciConformanceCorpusTests
{
    [Fact]
    public void Registered_inventory_has_exact_expected_count()
    {
        LciConformanceCorpus.Registrations.Should().HaveCount(LciConformanceCorpus.ExpectedCount);
        LciConformanceCorpus.Statuses.Should().HaveCount(LciConformanceCorpus.ExpectedCount);
    }

    [Fact]
    public void Registered_ids_and_classification_ids_are_unique()
    {
        LciConformanceCorpus.Registrations
            .Select(test => test.Id)
            .Should()
            .OnlyHaveUniqueItems();

        LciConformanceCorpus.Statuses
            .Select(status => status.Id)
            .Should()
            .OnlyHaveUniqueItems();
    }

    [Fact]
    public void Every_registered_case_has_all_declared_files()
    {
        foreach (LciTestRegistration test in LciConformanceCorpus.Registrations)
        {
            File.Exists(test.SourcePath).Should().BeTrue($"{test.Id} must have its LOLCODE source");

            if (test.ExpectedOutputPath is not null)
            {
                File.Exists(test.ExpectedOutputPath)
                    .Should().BeTrue($"{test.Id} must have its expected stdout");
            }

            if (test.InputPath is not null)
                File.Exists(test.InputPath).Should().BeTrue($"{test.Id} must have its stdin fixture");

            if (test.ExpectedErrorPath is not null)
            {
                File.Exists(test.ExpectedErrorPath)
                    .Should().BeTrue($"{test.Id} must preserve its upstream test.err fixture");
            }

            if (test.WorkingDirectoryPath is not null)
            {
                Directory.Exists(test.WorkingDirectoryPath)
                    .Should().BeTrue($"{test.Id} must have its working directory fixture");
            }

            (test.ExpectError ^ test.ExpectedOutputPath is not null)
                .Should().BeTrue($"{test.Id} must assert either failure or exact stdout");
        }
    }

    [Fact]
    public void Every_registered_case_has_one_valid_classification()
    {
        string[] registeredIds = LciConformanceCorpus.Registrations
            .Select(test => test.Id)
            .Order(StringComparer.Ordinal)
            .ToArray();
        string[] classifiedIds = LciConformanceCorpus.Statuses
            .Select(status => status.Id)
            .Order(StringComparer.Ordinal)
            .ToArray();

        classifiedIds.Should().Equal(registeredIds);

        foreach (LciTestStatus status in LciConformanceCorpus.Statuses)
        {
            status.Status.Should().BeOneOf("pass", "skip");
            if (status.Status == "skip")
            {
                status.Feature.Should().NotBeNullOrWhiteSpace(
                    $"{status.Id} must name the unsupported feature category");
                status.Reason.Should().NotBeNullOrWhiteSpace(
                    $"{status.Id} must explain the unsupported behavior");
            }
        }
    }

    [Fact]
    public void Complete_upstream_tree_matches_pinned_content()
    {
        string corpusRoot = Path.Combine(
            AppContext.BaseDirectory, "Conformance", "lci");
        string upstreamRoot = Path.Combine(corpusRoot, "upstream");
        string[] files = Directory.EnumerateFiles(
                upstreamRoot, "*", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .ToArray();

        files.Should().HaveCount(1376);

        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (string file in files)
        {
            string relativePath = Path.GetRelativePath(upstreamRoot, file).Replace('\\', '/');
            hash.AppendData(Encoding.UTF8.GetBytes(relativePath));
            hash.AppendData([0]);
            hash.AppendData(File.ReadAllBytes(file));
            hash.AppendData([0]);
        }

        string actualHash = Convert.ToHexStringLower(hash.GetHashAndReset());
        string expectedHash = File.ReadAllText(
            Path.Combine(corpusRoot, "upstream-tree.sha256")).Trim();
        actualHash.Should().Be(expectedHash);
    }
}
