using Lolcode.CodeAnalysis;

namespace Lolcode.CodeAnalysis.Tests;

public sealed class ProviderDiscoveryTests
{
    [Fact]
    public void DiscoversCustomProviderFromAssemblyMetadata()
    {
        ProviderDiscoveryResult result = ProviderDiscovery.Discover(
            [typeof(CustomProviderFixture.CustomLibrary).Assembly.Location]);

        result.Errors.Should().BeEmpty();
        result.Providers.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new LolcodeLibraryProviderDeclaration(
                "CUSTOM",
                "CustomProviderFixture",
                "CustomProviderFixture.CustomLibrary",
                false,
                1));
    }

    [Fact]
    public void RejectsMalformedDuplicateAndReservedProviderDeclarations()
    {
        ProviderDiscoveryResult result = ProviderDiscovery.Discover(
        [
            typeof(CustomProviderFixture.CustomLibrary).Assembly.Location,
            typeof(DuplicateProviderFixture.DuplicateLibrary).Assembly.Location,
            typeof(InvalidProviderFixture.InvalidLibrary).Assembly.Location,
            typeof(LoaderFixtures.STRING).Assembly.Location,
        ]);

        result.Errors.Should().Contain(error => error.Contains(
            "Duplicate LOLCODE provider name 'CUSTOM'",
            StringComparison.Ordinal));
        result.Errors.Should().Contain(error => error.Contains(
            "invalid constructor arguments",
            StringComparison.Ordinal));
        result.Errors.Should().Contain(error => error.Contains(
            "attempts to replace the reserved built-in provider",
            StringComparison.Ordinal));
        result.Errors.Should().Contain(error => error.Contains(
            "unsupported contract version 2",
            StringComparison.Ordinal));
    }
}
