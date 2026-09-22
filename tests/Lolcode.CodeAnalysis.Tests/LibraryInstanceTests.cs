using Lolcode.Runtime;

namespace Lolcode.CodeAnalysis.Tests;

public sealed class LibraryInstanceTests
{
    [Fact]
    public void IdenticalLibraryDefinitionsFromDuplicateRuntimePaths_AreIdempotent()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "lolcode-library-discovery",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string runtimePath = typeof(LolRuntime).Assembly.Location;
            string duplicatePath = Path.Combine(directory, "Lolcode.Runtime.dll");
            File.Copy(runtimePath, duplicatePath);

            LibraryDiscoveryResult result = LibraryDiscovery.Discover(
                [runtimePath, duplicatePath],
                runtimePath);

            result.Diagnostics.Should().BeEmpty();
            result.Definitions.Select(definition => definition.Name)
                .Should().OnlyHaveUniqueItems();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void OrdinaryLibraryProperty_IsNotTreatedAsGeneratedLibraryState()
    {
        var scope = LolRuntime.CreateScope();
        Type type = typeof(CustomProviderFixture.PropertyLibrary);
        scope.Libraries.Register(
            "PROPERTY_LIBRARY",
            type.Assembly.GetName().Name!,
            type.FullName!);

        LolRuntime.LoadLibrary(scope, "PROPERTY_LIBRARY");

        LolRuntime.Invoke(scope, ["PROPERTY_LIBRARY"], ["ECHO"], ["HAI"])
            .Should().Be("HAI");
    }
}
