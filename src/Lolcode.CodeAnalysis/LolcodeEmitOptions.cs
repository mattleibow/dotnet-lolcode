namespace Lolcode.CodeAnalysis;

/// <summary>Controls how emitted applications resolve LOLCODE library imports.</summary>
public enum LolcodeLibraryResolution
{
    /// <summary>Lets the host choose the existing dynamic resolution behavior.</summary>
    Dynamic,

    /// <summary>Uses only direct factories declared by the project's resolved references.</summary>
    Static,
}

/// <summary>
/// Immutable options for project-hosted LOLCODE emission.
/// NativeAOT remains an SDK publish operation; this type controls only managed IL emission.
/// </summary>
public sealed record LolcodeEmitOptions
{
    /// <summary>Gets the library resolution path used by generated managed IL.</summary>
    public LolcodeLibraryResolution LibraryResolution { get; init; } =
        LolcodeLibraryResolution.Dynamic;

    /// <summary>
    /// Gets whether the hosting SDK owns runtime configuration and deployment artifacts.
    /// Direct path-based <see cref="LolcodeCompilation.Emit(string,string)"/> keeps its
    /// historical runtimeconfig behavior.
    /// </summary>
    public bool RuntimeConfigOwnedByHost { get; init; }
}
