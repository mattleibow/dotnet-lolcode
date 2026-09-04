namespace Lolcode.EndToEnd.Tests;

public class LciConformanceCorpusTests
{
    [Fact]
    public void Registered_inventory_is_not_empty()
    {
        LciConformanceCorpus.Registrations.Should().NotBeEmpty();
    }

    [Fact]
    public void Registered_ids_are_unique()
    {
        LciConformanceCorpus.Registrations
            .Select(test => test.Id)
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

}
