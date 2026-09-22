namespace Lolcode.Runtime;

/// <summary>
/// Declares an assembly as a provider for a named LOLCODE <c>CAN HAS</c> library.
/// The compiler reads this metadata without loading the provider into the build host.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class LolcodeLibraryProviderAttribute : Attribute
{
    /// <summary>The currently supported provider metadata contract version.</summary>
    public const int CurrentContractVersion = 1;

    /// <summary>
    /// Initializes a provider declaration.
    /// </summary>
    /// <param name="lolName">The LOLCODE name imported by <c>CAN HAS</c>.</param>
    /// <param name="providerType">The static type that exports provider methods.</param>
    /// <param name="isBuiltIn">Whether this is a reserved SDK-bundled provider.</param>
    /// <param name="contractVersion">The provider metadata contract version.</param>
    public LolcodeLibraryProviderAttribute(
        string lolName,
        Type providerType,
        bool isBuiltIn = false,
        int contractVersion = CurrentContractVersion)
    {
        LolName = lolName;
        ProviderType = providerType;
        IsBuiltIn = isBuiltIn;
        ContractVersion = contractVersion;
    }

    /// <summary>Gets the LOLCODE name imported by <c>CAN HAS</c>.</summary>
    public string LolName { get; }

    /// <summary>Gets the static type that exports provider methods.</summary>
    public Type ProviderType { get; }

    /// <summary>Gets whether the provider name is reserved by the SDK.</summary>
    public bool IsBuiltIn { get; }

    /// <summary>Gets the provider metadata contract version.</summary>
    public int ContractVersion { get; }
}
