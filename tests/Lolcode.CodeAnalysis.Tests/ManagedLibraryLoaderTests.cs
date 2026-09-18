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
            LolRuntime.LoadLibrary(scope, "STRING");

            LolRuntime.Invoke(scope, ["STRING"], ["LEN"], ["HAI"]).Should().Be(3);
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
