using System.Reflection;
using Lolcode.Runtime;

namespace Lolcode.CodeAnalysis.Tests
{
    /// <summary>Tests managed <c>CAN HAS</c> library discovery and invocation.</summary>
    public sealed class ManagedLibraryLoaderTests
    {
        [Fact]
        public void SingleNamespacedStaticType_IsSelectedRegardlessOfImportName()
        {
            Type? selected = LolRuntime.SelectManagedLibraryType(
                [typeof(LoaderFixtures.Ordinary.TextTools)],
                "UnrelatedAssemblyName");

            selected.Should().Be(typeof(LoaderFixtures.Ordinary.TextTools));
        }

        [Fact]
        public void MultipleStaticTypes_SelectTheSingleMatchingSimpleName()
        {
            Type? selected = LolRuntime.SelectManagedLibraryType(
                [typeof(LoaderFixtures.Ordinary.TextTools), typeof(LoaderFixtures.Named.ManagedTextPackage)],
                "ManagedTextPackage");

            selected.Should().Be(typeof(LoaderFixtures.Named.ManagedTextPackage));
        }

        [Fact]
        public void AmbiguousOrNestedStaticTypes_AreIgnored()
        {
            LolRuntime.SelectManagedLibraryType(
                    [typeof(LoaderFixtures.First.ManagedTextPackage),
                        typeof(LoaderFixtures.Second.ManagedTextPackage)],
                    "ManagedTextPackage")
                .Should().BeNull();

            LolRuntime.SelectManagedLibraryType(
                    [typeof(LoaderFixtures.Outer.NestedExport)],
                    "NestedExport")
                .Should().BeNull();
        }

        [Fact]
        public void ASingleMarkedGeneratedExport_IsSelectedRegardlessOfAssemblyOrTypeName()
        {
            LolRuntime.SelectManagedLibraryType(
                    [typeof(MarkedExports), typeof(LoaderFixtures.Ordinary.TextTools)],
                    "UnrelatedAssemblyName")
                .Should().Be(typeof(MarkedExports));

            LolRuntime.SelectManagedLibraryType(
                    [typeof(MarkedExports), typeof(SecondMarkedExports)],
                    "UnrelatedAssemblyName")
                .Should().BeNull();
        }

        [Fact]
        public void ContextInjection_IsLimitedToOneFirstRegisteredParameter()
        {
            MethodInfo first = typeof(ContextFixtures).GetMethod(nameof(ContextFixtures.First))!;
            MethodInfo second = typeof(ContextFixtures).GetMethod(nameof(ContextFixtures.Second))!;

            LolRuntime.IsSupportedManagedMethod(first).Should().BeFalse();
            LolRuntime.IsSupportedManagedMethod(first, allowContext: true).Should().BeTrue();
            LolRuntime.IsSupportedManagedMethod(second, allowContext: true).Should().BeFalse();
        }

        [Theory]
        [InlineData("3", typeof(int), 3)]
        [InlineData(3, typeof(string), "3")]
        [InlineData(1, typeof(bool), true)]
        [InlineData("2.5", typeof(double), 2.5)]
        public void ManagedArguments_AreConvertedToPrimitiveClrTypes(
            object value,
            Type targetType,
            object expected)
        {
            LolRuntime.ConvertManagedArgument(value, targetType).Should().Be(expected);
        }

        [Fact]
        public void ManagedInvocation_ConvertsArgumentsAndSurfacesTargetFailures()
        {
            MethodInfo repeat = typeof(LoaderFixtures.Ordinary.TextTools)
                .GetMethod(nameof(LoaderFixtures.Ordinary.TextTools.Repeat))!;
            object? result = LolRuntime.InvokeManagedMethod(
                repeat,
                repeat.GetParameters(),
                ["HAI", "3"]);

            result.Should().Be("HAI HAI HAI");

            MethodInfo throws = typeof(LoaderFixtures.Ordinary.TextTools)
                .GetMethod(nameof(LoaderFixtures.Ordinary.TextTools.Throws))!;
            FluentActions.Invoking(() => LolRuntime.InvokeManagedMethod(
                    throws,
                    throws.GetParameters(),
                    []))
                .Should().Throw<LolRuntimeException>()
                .WithMessage("managed failure");
        }

        [Fact]
        public void Loader_ExportsSupportedMethodsAndRejectsAmbiguousOrUnsupportedMethods()
        {
            var scope = LolRuntime.CreateScope();
            LolRuntime.LoadLibrary(scope, "ManagedTestPackage");

            LolRuntime.Invoke(scope, ["ManagedTestPackage"], ["Echo"], ["HAI"])
                .Should().Be("HAI");
            LolRuntime.Invoke(scope, ["ManagedTestPackage"], ["Noop"], [])
                .Should().BeNull();
            FluentActions.Invoking(() => LolRuntime.Invoke(
                    scope,
                    ["ManagedTestPackage"],
                    ["Overloaded"],
                    [1]))
                .Should().Throw<LolRuntimeException>()
                .WithMessage("Binding does not exist: Overloaded");
            FluentActions.Invoking(() => LolRuntime.Invoke(
                    scope,
                    ["ManagedTestPackage"],
                    ["WithOut"],
                    [1]))
                .Should().Throw<LolRuntimeException>()
                .WithMessage("Binding does not exist: WithOut");
        }

        [Fact]
        public void BuiltInLibrary_TakesPrecedenceOverSameNamedManagedAssembly()
        {
            var scope = LolRuntime.CreateScope();
            scope.Libraries.Register(
                "STRING",
                "Lolcode.Runtime.String",
                "Lolcode.Runtime.String.StringLibrary",
                isBuiltIn: true,
                1);
            LolRuntime.LoadLibrary(scope, "STRING");

            LolRuntime.Invoke(scope, ["STRING"], ["LEN"], ["HAI"]).Should().Be(3);
        }

        [Fact]
        public void CustomAlias_DoesNotReceiveTrustedLibraryContext()
        {
            var scope = LolRuntime.CreateScope();
            Type type = typeof(CustomProviderFixture.CustomLibrary);
            scope.Libraries.Register(
                "CUSTOM_ALIAS",
                type.Assembly.GetName().Name!,
                type.FullName!,
                isBuiltIn: false,
                1);

            LolRuntime.LoadLibrary(scope, "CUSTOM_ALIAS");

            LolRuntime.Invoke(scope, ["CUSTOM_ALIAS"], ["ECHO"], ["HAI"])
                .Should().Be("CUSTOM HAI");
            FluentActions.Invoking(() =>
                    LolRuntime.Invoke(scope, ["CUSTOM_ALIAS"], ["CONTEXT"], []))
                .Should().Throw<LolRuntimeException>()
                .WithMessage("Binding does not exist: CONTEXT");
        }

        [Fact]
        public void ManagedLibraryPath_AcceptsSimpleAssemblyNamesWithinBaseDirectory()
        {
            LolRuntime.TryGetManagedLibraryPath("Managed.Text-Package", out string path)
                .Should().BeTrue();

            Path.GetFileName(path).Should().Be("Managed.Text-Package.dll");
            Path.GetDirectoryName(path).Should().Be(
                Path.TrimEndingDirectorySeparator(
                    Path.GetFullPath(AppContext.BaseDirectory)));
        }

        [Fact]
        public void ManagedLibraryPath_RejectsDynamicPathFormsWithoutLoadingOutsideAssembly()
        {
            string assemblyPath = typeof(LoaderFixtures.ManagedTestPackage).Assembly.Location;
            string externalName = $"outside-{Guid.NewGuid():N}";
            string externalPath = Path.Combine(
                Path.GetDirectoryName(
                    Path.TrimEndingDirectorySeparator(
                        Path.GetFullPath(AppContext.BaseDirectory)))!,
                $"{externalName}.dll");
            File.Copy(assemblyPath, externalPath);

            try
            {
                string[] names =
                [
                    string.Empty,
                    ".",
                    "..",
                    $"../{externalName}",
                    $"..\\{externalName}",
                    Path.GetFullPath(externalPath),
                    $"C:\\{externalName}",
                ];
                foreach (string name in names)
                {
                    LolRuntime.TryGetManagedLibraryPath(name, out _).Should().BeFalse();

                    var scope = LolRuntime.CreateScope();
                    LolRuntime.LoadLibrary(scope, name);
                    FluentActions.Invoking(() => LolRuntime.GetValue(scope, [name]))
                        .Should().Throw<LolRuntimeException>()
                        .WithMessage("*does not exist*");
                }

            }
            finally
            {
                File.Delete(externalPath);
            }
        }

        [Theory]
        [InlineData(nameof(LoaderFixtures.Unsupported.Generic))]
        [InlineData(nameof(LoaderFixtures.Unsupported.Ref))]
        [InlineData(nameof(LoaderFixtures.Unsupported.Decimal))]
        public void UnsupportedManagedMethods_AreRejected(string methodName)
        {
            MethodInfo method = typeof(LoaderFixtures.Unsupported).GetMethod(methodName)!;

            LolRuntime.IsSupportedManagedMethod(method).Should().BeFalse();
        }
    }
}

[LolcodeLibrary]
public static class MarkedExports;

[LolcodeLibrary]
public static class SecondMarkedExports;

public static class ContextFixtures
{
    public static int First(LolcodeLibraryContext context, int value) => value;

    public static int Second(int value, LolcodeLibraryContext context) => value;
}

namespace LoaderFixtures.Ordinary
{
    public static class TextTools
    {
        public static string Repeat(string value, int count) =>
            string.Join(" ", Enumerable.Repeat(value, count));

        public static void Throws() => throw new InvalidOperationException("managed failure");
    }
}

namespace LoaderFixtures.Named
{
    public static class ManagedTextPackage
    {
    }
}

namespace LoaderFixtures.First
{
    public static class ManagedTextPackage
    {
    }
}

namespace LoaderFixtures.Second
{
    public static class ManagedTextPackage
    {
    }
}

namespace LoaderFixtures
{
    public sealed class Outer
    {
        public static class NestedExport
        {
        }
    }

    public static class Unsupported
    {
        public static T Generic<T>(T value) => value;

        public static int Ref(ref int value) => value;

        public static decimal Decimal(decimal value) => value;
    }
}
