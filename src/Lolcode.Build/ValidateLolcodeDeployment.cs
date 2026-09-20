using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.Build.Framework;

namespace Lolcode.Build;

/// <summary>
/// Validates that an existing LOLCODE assembly was compiled for the deployment
/// mode requested by an MSBuild publish invocation.
/// </summary>
public sealed class ValidateLolcodeDeployment : Microsoft.Build.Utilities.Task
{
    /// <summary>Gets or sets the managed assembly that will be published.</summary>
    [Required]
    public string AssemblyPath { get; set; } = "";

    /// <summary>Gets or sets the expected library-resolution mode.</summary>
    [Required]
    public string ExpectedLibraryResolution { get; set; } = "";

    /// <inheritdoc/>
    public override bool Execute()
    {
        if (!File.Exists(AssemblyPath))
        {
            LogMismatch("missing");
            return false;
        }

        try
        {
            using var stream = File.OpenRead(AssemblyPath);
            using var peReader = new PEReader(stream);
            MetadataReader metadata = peReader.GetMetadataReader();
            string actual = HasStaticLibraryAttribute(metadata)
                ? "Static"
                : "Dynamic";
            if (!string.Equals(
                actual,
                ExpectedLibraryResolution,
                StringComparison.OrdinalIgnoreCase))
            {
                LogMismatch(actual);
                return false;
            }

            return true;
        }
        catch (Exception exception) when (
            exception is BadImageFormatException or InvalidOperationException or
            IOException or UnauthorizedAccessException)
        {
            Log.LogError(
                subcategory: null,
                errorCode: "LOL3009",
                helpKeyword: null,
                file: AssemblyPath,
                lineNumber: 0,
                columnNumber: 0,
                endLineNumber: 0,
                endColumnNumber: 0,
                message: "Unable to validate existing LOLCODE output for publish: {0}",
                exception.Message);
            return false;
        }
    }

    private void LogMismatch(string actual)
    {
        Log.LogError(
            subcategory: null,
            errorCode: "LOL3009",
            helpKeyword: null,
            file: AssemblyPath,
            lineNumber: 0,
            columnNumber: 0,
            endLineNumber: 0,
            endColumnNumber: 0,
            message:
                "Existing LOLCODE output was compiled for '{0}' library resolution, " +
                "but this publish requires '{1}'. Rebuild without --no-build.",
            actual,
            ExpectedLibraryResolution);
    }

    private static bool HasStaticLibraryAttribute(MetadataReader metadata)
    {
        foreach (CustomAttributeHandle handle in metadata.GetAssemblyDefinition().GetCustomAttributes())
        {
            CustomAttribute attribute = metadata.GetCustomAttribute(handle);
            if (IsStaticLibraryAttribute(metadata, attribute.Constructor))
                return true;
        }

        return false;
    }

    private static bool IsStaticLibraryAttribute(
        MetadataReader metadata,
        EntityHandle constructor)
    {
        EntityHandle type = constructor.Kind switch
        {
            HandleKind.MemberReference =>
                metadata.GetMemberReference((MemberReferenceHandle)constructor).Parent,
            HandleKind.MethodDefinition =>
                metadata.GetMethodDefinition((MethodDefinitionHandle)constructor).GetDeclaringType(),
            _ => default,
        };

        return type.Kind switch
        {
            HandleKind.TypeReference =>
                IsStaticLibraryType(metadata, metadata.GetTypeReference((TypeReferenceHandle)type)),
            HandleKind.TypeDefinition =>
                IsStaticLibraryType(metadata, metadata.GetTypeDefinition((TypeDefinitionHandle)type)),
            _ => false,
        };
    }

    private static bool IsStaticLibraryType(
        MetadataReader metadata,
        TypeReference type) =>
        metadata.GetString(type.Namespace) == "Lolcode.Runtime" &&
        metadata.GetString(type.Name) == "LolcodeStaticLibraryAttribute";

    private static bool IsStaticLibraryType(
        MetadataReader metadata,
        TypeDefinition type) =>
        metadata.GetString(type.Namespace) == "Lolcode.Runtime" &&
        metadata.GetString(type.Name) == "LolcodeStaticLibraryAttribute";
}
