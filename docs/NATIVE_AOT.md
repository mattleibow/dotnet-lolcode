# Publishing LOLCODE applications

LOLCODE projects emit managed IL. NativeAOT, trimming, and single-file bundling
are performed afterwards by the standard .NET SDK publish pipeline; the
compiler never invokes `ilc` and does not produce native code itself.

## Commands

Use a `.lolproj` application, a RID, and the normal SDK properties:

```console
# Framework-dependent managed bundle (requires the target .NET runtime)
dotnet publish MyApp.lolproj -c Release -r osx-arm64 -p:PublishSingleFile=true --self-contained false

# Self-contained trimmed managed bundle
dotnet publish MyApp.lolproj -c Release -r linux-x64 -p:PublishTrimmed=true --self-contained true

# RID-specific NativeAOT executable
dotnet publish MyApp.lolproj -c Release -r osx-arm64 -p:PublishAot=true --self-contained true
```

NativeAOT output is not cross-platform. It is a native executable for the
selected RID and has no required managed DLL, `.deps.json`, or
`.runtimeconfig.json` sidecars. Native debug symbols and operating-system
dependencies are separate artifacts. A managed single-file app is still a
managed bundle and may require an installed runtime.

## Static import contract

`LolcodeLibraryResolution` defaults to `Auto`. It resolves to `Dynamic` for
ordinary builds and to `Static` when `PublishTrimmed`, `PublishSingleFile`, or
`PublishAot` is enabled. Explicitly setting `Dynamic` with any of those
properties fails with `LOL3005`.

Static registrations use direct factory delegates from declared
project/package references. They never use `Assembly.Load`, `Assembly.LoadFrom`,
filesystem probing, member discovery, or a dynamic fallback. The official
providers expose these registration facades: STRING, STDLIB, STDIO, and SOCKS.
The compiler still emits managed IL and the SDK owns apphost, runtimeconfig,
deps, bundle, and publish artifacts.

An undeclared `CAN HAS` name fails at runtime with an explicit static-contract
error instead of probing a sidecar DLL. Old or custom provider descriptors
without a public static factory fail the build with `LOL3003`.

## Provider authoring

A static-capable provider exposes a public factory such as:

```csharp
public static class ExampleLibraryFactory
{
    public static LolObject Create(LolScope scope)
    {
        var builder = new LolcodeLibraryBuilder(scope);
        builder.AddFunction("HELLO", 0, static (_, _) => "HAI");
        return builder.Build();
    }
}
```

The package's `buildTransitive` descriptor must include the factory's fully
qualified name as its sixth field. Factories must create state per import,
register BLOBs through `LolcodeLibraryContext`, and use direct calls only.
The first five descriptor fields remain compatible with dynamic builds.

## Boundaries and troubleshooting

- Dynamic adjacent plugins, late-installed providers, and `LolcodeScript.Run`
  are CoreCLR-only. They use reflection/filesystem loading and are explicitly
  excluded from trimmed and NativeAOT applications.
- Generated LOLCODE library modules and arbitrary managed exports still require
  their own static factory descriptor; absent metadata is diagnosed rather
  than dynamically loaded.
- `LOL3005` means a deployment property was combined with dynamic resolution.
  Set `LolcodeLibraryResolution=Static` (or leave it `Auto`).
- `LOL3003` means the resolved provider is an old/dynamic-only package. Update
  the package or add a public factory and sixth descriptor field.
- `LOL3001` is reserved for source imports that are not present in the declared
  static closure. Add the package/project reference; do not copy a DLL next to
  the executable.

The direct path preserves normal LOLCODE value coercion, dynamic values inside
the declared closure, scoped BLOB ownership, raw-byte output, and flushing.
Ordinary framework-dependent applications keep the existing dynamic loader by
default.
