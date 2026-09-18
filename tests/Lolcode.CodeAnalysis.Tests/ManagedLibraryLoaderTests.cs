using System.Reflection;
using Lolcode.Runtime;

namespace Lolcode.CodeAnalysis.Tests
{
    /// <summary>Tests metadata-based managed <c>CAN HAS</c> library selection.</summary>
    public sealed class ManagedLibraryLoaderTests
    {
        [Fact]
        public void MarkedNamespacedType_IsSelectedRegardlessOfAssemblyImportName()
        {
            Type? selected = LolRuntime.SelectManagedLibraryType(
                [typeof(LoaderFixtures.Marked.DifferentExportName), typeof(LoaderFixtures.Legacy.NamedForImport)],
                "NamedForImport");

            selected.Should().Be(typeof(LoaderFixtures.Marked.DifferentExportName));
        }

        [Fact]
        public void NestedMarkedType_IsIgnored()
        {
            Type? selected = LolRuntime.SelectManagedLibraryType(
                [typeof(LoaderFixtures.Outer.NestedExport)],
                "NestedExport");

            selected.Should().BeNull();
        }

        [Fact]
        public void MultipleMarkedTypes_AreAmbiguous()
        {
            Type? selected = LolRuntime.SelectManagedLibraryType(
                [typeof(LoaderFixtures.Marked.DifferentExportName), typeof(LoaderFixtures.Marked.SecondExport)],
                "Anything");

            selected.Should().BeNull();
        }

        [Fact]
        public void MultipleLegacyFallbackTypes_AreAmbiguous()
        {
            Type? selected = LolRuntime.SelectManagedLibraryType(
                [typeof(LoaderFixtures.First.LegacyExport), typeof(LoaderFixtures.Second.LegacyExport)],
                "LegacyExport");

            selected.Should().BeNull();
        }

        [Fact]
        public void SingleUnmarkedNamespacedType_IsSelected()
        {
            Type? selected = LolRuntime.SelectManagedLibraryType(
                [typeof(LoaderFixtures.Ordinary.TextTools)],
                "UnrelatedAssemblyName");

            selected.Should().Be(typeof(LoaderFixtures.Ordinary.TextTools));
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
        public void SupportedManagedMethod_AcceptsPrimitiveSignature()
        {
            MethodInfo method = typeof(LoaderFixtures.Ordinary.TextTools)
                .GetMethod(nameof(LoaderFixtures.Ordinary.TextTools.Repeat))!;

            LolRuntime.IsSupportedManagedMethod(method).Should().BeTrue();
        }
    }
}

namespace LoaderFixtures.Marked
{
    [LolcodeLibrary]
    public static class DifferentExportName
    {
    }

    [LolcodeLibrary]
    public static class SecondExport
    {
    }
}

namespace LoaderFixtures.Legacy
{
    public static class NamedForImport
    {
    }
}

namespace LoaderFixtures.First
{
    public static class LegacyExport
    {
    }
}

namespace LoaderFixtures.Second
{
    public static class LegacyExport
    {
    }
}

namespace LoaderFixtures
{
    public sealed class Outer
    {
        [LolcodeLibrary]
        public static class NestedExport
        {
        }
    }
}

namespace LoaderFixtures.Ordinary
{
    public static class TextTools
    {
        public static string Repeat(string value, int count) =>
            string.Concat(Enumerable.Repeat(value, count));
    }
}
