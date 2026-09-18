using System.Reflection;
using Lolcode.CodeAnalysis.Errors;

namespace Lolcode.CodeAnalysis.Tests;

public sealed class DiagnosticCatalogTests
{
    [Fact]
    public void EveryDiagnosticDescriptorHasAnErrorCode()
    {
        var descriptorIds = typeof(DiagnosticDescriptors)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(DiagnosticDescriptor))
            .Select(field => ((DiagnosticDescriptor)field.GetValue(null)!).Id);
        var errorCodeIds = Enum.GetNames<ErrorCode>();

        descriptorIds.Should().BeSubsetOf(errorCodeIds);
    }
}
