using Lolcode.Runtime;

namespace Lolcode.CodeAnalysis.Tests;

public sealed class LibraryInstanceTests
{
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
