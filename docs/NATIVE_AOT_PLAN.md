# NativeAOT, Trimming, and Single-File Publishing

This document records the implementation plan for publishing supported LOLCODE
applications through the standard .NET SDK deployment pipeline. It is a design
and progress record for the NativeAOT work stacked on the reusable game
frameworks in PR #16.

## Goals

- Preserve the existing compiler pipeline and managed IL output.
- Keep ordinary framework-dependent and dynamic-library builds working.
- Make supported `.lolproj` applications publishable with standard commands:

  ```console
  dotnet publish -r <RID> -c Release -p:PublishAot=true
  ```

- Produce one RID-specific native executable with no required managed DLL,
  `.deps.json`, `.runtimeconfig.json`, or installed .NET runtime where the
  application and its libraries satisfy the static deployment contract.
- Support trimming and managed single-file publishing through the corresponding
  .NET SDK properties.
- Diagnose unsupported dynamic/plugin scenarios instead of silently disabling
  requested deployment features or falling back to runtime discovery.
- Validate at least one representative game application from PR #16.

Native shared libraries and unmanaged exports are not part of this work. The
existing public LOLCODE wrappers use managed `object` and `LolObject` contracts;
they are not an unmanaged ABI.

## Baseline findings

### Managed IL remains the publishing input

`CodeGenerator` emits ordinary CLI metadata and IL with
`PersistedAssemblyBuilder`, including a public managed entry point for
applications and public wrappers plus `__CreateLolcodeLibrary(LolScope)` for
libraries. `PersistedAssemblyBuilder` runs in the compiler at build time; the
generated application does not require it at runtime.

NativeAOT therefore belongs at application publish time, after `CoreCompile`,
rather than in `LolcodeCompilation.Emit`. The compiler will continue producing a
managed intermediate assembly, and Microsoft.NET.Sdk will remain responsible
for trimming, bundling, and native compilation.

### Dynamic LOLCODE values are compatible with a closed AOT application

BUKKIT/SRS storage, first-class functions, scope dictionaries, boxing, and
delegates created with `ldftn` do not inherently require runtime code generation.
They can remain dynamic inside a statically known application dependency graph.

### Runtime library discovery is the principal blocker

The current `LolRuntime.LoadLibrary` path uses:

- `Assembly.Load` for registered package providers;
- `Assembly.LoadFrom` and physical files for adjacent managed libraries;
- runtime type/member discovery and `MethodInfo.Invoke`;
- reflective discovery and invocation of generated
  `__CreateLolcodeLibrary` factories.

Those mechanisms do not create reliable linker reachability and conflict with
single-file execution when a bundled managed assembly has no adjacent physical
DLL. The supported trimming/NativeAOT path must use explicit assembly references
and direct calls.

### Byte output contains an unrelated trimming hazard

`YarnByteSink` currently searches private `TextWriter` fields to locate an
underlying stream. This is fragile under trimming and runtime implementation
changes. The implementation must move to explicit byte-output capabilities while
preserving exact bytes, scoped I/O, flushing, and ownership.

### SDK deployment artifacts must be SDK-owned

The SDK already imports Microsoft.NET.Sdk and overrides `CoreCompile` to produce
`@(IntermediateAssembly)`. Project builds and publishes must let the SDK own
`.deps.json`, `.runtimeconfig.json`, apphost, single-file, and NativeAOT
artifacts. The existing standalone path-based `Emit` behavior remains available
for API callers.

## Supported deployment model

| Mode | Library resolution | Result |
|---|---|---|
| Ordinary framework-dependent build | Dynamic by default | Existing managed behavior |
| Untrimmed self-contained build | Dynamic or static | Managed application and runtime files |
| Framework-dependent single-file | Static for bundled imports | One bundle requiring installed .NET |
| Self-contained single-file | Static for bundled imports | One managed/runtime bundle |
| Trimmed publish | Static | Reduced managed output or bundle |
| NativeAOT publish | Static | RID-specific native executable |
| Late-installed or adjacent plugin | Dynamic only | Explicit external sidecar; not trim/AOT-safe |
| Managed LOLCODE class library | Static-compatible when requested | Managed input to a final application publish |

For this feature, a "single native binary LOLCODE app" means:

> A RID-specific native executable containing the application and its supported
> managed dependency closure, requiring neither JIT compilation nor an installed
> .NET runtime and requiring no managed assembly extraction at execution time.

Native debug symbols, operating-system libraries, application data, save files,
shell commands, and deliberately external native dependencies are separate
artifacts or platform requirements. NativeAOT is not one cross-platform binary,
and normal single-file publishing is not equivalent to NativeAOT.

## Architecture decisions

### 1. Keep the IL backend and use standard SDK publishing

The implementation will not add a C# transpiler, custom `ilc` invocation, IL
merger, or `EmitNative` API. `CoreCompile` continues producing the managed
intermediate assembly consumed by the SDK's normal publish targets.

### 2. Separate static imports from dynamic loading

The runtime will expose a reflection-free static registration/import path with a
separate call graph from the legacy loader:

- **Dynamic mode** preserves package descriptors, adjacent managed plugins,
  unknown-import behavior, and existing JIT-oriented APIs.
- **Static mode** imports only factories and adapters resolved from declared
  build references. It performs no filesystem probing, assembly loading, or
  member discovery and reports missing registrations explicitly.

Static mode must not call a method that contains a dynamic fallback. Keeping
the call graphs separate makes the supported path analyzable and prevents a
runtime option from merely hiding linker-incompatible code.

### 3. Register lazy factories, not global provider instances

Static registrations are scoped, lazy, conflict-checked, and idempotent for the
same normalized contract. Each import receives the same per-import state and
resource ownership semantics as today. Provider state must not become mutable
process-global state.

Official providers will expose public factory facades from their existing
assemblies while keeping implementation types internal.

### 4. Emit direct adapters for supported libraries

The compiler/build resolver will inspect metadata without executing referenced
code and emit:

- direct calls to official provider factory facades;
- direct calls to generated LOLCODE `__CreateLolcodeLibrary` factories;
- typed adapters for supported public static managed-library methods.

Adapters retain existing coercion, `void` to NOOB, error wrapping, resource
tracking, and returned-resource ownership transfer behavior.

Dynamic import names remain possible in a statically closed application when
the project declares the finite candidate set. An undeclared runtime name fails
with a static-mode diagnostic or explicit runtime error; it does not probe disk.

### 5. Keep compatibility claims honest

Runtime and provider projects will enable the standard trimming and NativeAOT
analyzers. Compatibility metadata is emitted only for assemblies built with the
static contract and validated by publish tests.

Legacy discovery APIs receive narrow `RequiresUnreferencedCode`,
`RequiresAssemblyFiles`, or `RequiresDynamicCode` annotations where their actual
behavior requires them. Blanket linker roots and warning suppressions are not
the primary design.

### 6. Treat compiler hosting and script execution separately

`LolcodeScript.Run` loads generated assemblies into CoreCLR and is not part of
the supported NativeAOT application model. Parsing, binding, and persisted IL
emission remain compiler capabilities; runtime loading/execution keeps its
existing CoreCLR contract and is documented and annotated separately.

## Compiler and runtime contracts

The implementation should preserve current public overloads and add immutable,
documented options and references only where needed:

- compilation/emission options for output kind, target references, debug format,
  runtimeconfig ownership, and library resolution;
- file-backed metadata references and resolved library references;
- a `Dynamic` or `Static` library-resolution mode, with MSBuild resolving its
  public `Auto` setting before invoking the compiler;
- a public runtime factory delegate and immutable registration descriptor;
- explicit `RegisterLibraries` and `ImportRegisteredLibrary` methods;
- a provider/library builder that uses existing scope and resource semantics
  without exposing mutable runtime internals.

Existing `LolcodeCompilation.Create`, `GetDiagnostics`, and `Emit` calls retain
their current defaults. Existing managed artifacts and dynamic descriptors
remain consumable by ordinary JIT deployments.

Generated static-compatible libraries include versioned build-time metadata for
their factory ABI, dependencies, resolution mode, and any declared dynamic
import candidates. Runtime code does not reflect over this metadata.

## MSBuild contract

The public properties use standard .NET meanings:

| Property | Behavior |
|---|---|
| `PublishAot` | Requests NativeAOT for a final executable |
| `PublishTrimmed` | Requests trimming and requires static imports |
| `PublishSingleFile` | Requests the standard managed bundle |
| `SelfContained` / `PublishSelfContained` | Retains SDK behavior |
| `RuntimeIdentifier` | Retains SDK selection and validation |
| `IsAotCompatible` / `IsTrimmable` | Requests compatible library compilation and metadata |
| `LolcodeLibraryResolution` | `Auto`, `Dynamic`, or `Static` |
| `LolcodeRequireSingleBinary` | Optionally validates the final artifact contract |

`Auto` resolves to `Dynamic` for ordinary builds and `Static` when trimming,
NativeAOT, or bundled imports require a closed dependency graph. Explicitly
incompatible combinations fail with actionable diagnostics instead of changing
the user's publish properties.

Focused targets will:

1. validate LOLCODE-specific deployment combinations;
2. resolve provider/module/managed-library contracts from resolved references;
3. fingerprint sources, references, debug format, effective resolution mode,
   and deployment metadata for incremental builds;
4. pass the resolved contracts to `Lolc` during `CoreCompile`;
5. validate that `--no-build` publish output matches requested deployment
   options;
6. optionally validate final loose managed/native/content artifacts.

They will not override `Publish`, ILLink, NativeAOT, or bundle implementation
targets. Design-time builds retain `SkipCompilerExecution` and do not require a
native toolchain.

## Diagnostics

Deployment and static-link failures use a dedicated `LOL3xxx` range:

| ID | Meaning |
|---|---|
| `LOL3001` | Static import cannot be resolved from declared references |
| `LOL3002` | Alias, descriptor, or reserved-name conflict |
| `LOL3003` | Missing, inaccessible, or incompatible static factory |
| `LOL3004` | Selected managed export cannot be adapted |
| `LOL3005` | Dynamic resolution requested with trimming or NativeAOT |
| `LOL3006` | Referenced LOLCODE module requires the legacy dynamic loader |
| `LOL3007` | Dynamic import has no declared candidate set |
| `LOL3008` | Strict single-binary publish requires external files |
| `LOL3009` | Existing output does not match requested deployment options |
| `LOL3010` | Managed wrappers cannot be published as unmanaged exports |

Source imports receive `.lol` locations. Reference diagnostics identify the
import, assembly, expected contract, and remedy. Standard `NETSDK`, `ILLink`,
and NativeAOT diagnostics remain visible.

## Implementation phases

### Phase 0: Baseline and decision record

- Add publish test infrastructure for ordinary, single-file, trimmed, and
  NativeAOT outputs.
- Prove the SDK consumes the emitted entry assembly without a generated C# host.
- Record file-based application property precedence and supported .NET 10 SDK
  requirements.

**Acceptance:** managed behavior is captured before production changes; publish
fixtures run from relocated output and distinguish observed failures from
anticipated risks.

### Phase 1: AOT-safe runtime and provider factories

- Add reflection-free registration/import primitives.
- Add public factory facades to STRING, STDLIB, STDIO, and SOCKS.
- Share invocation and ownership semantics with the dynamic path.
- Replace private-field `TextWriter` discovery with explicit byte sinks.
- Enable trimming/AOT analyzers and annotate legacy boundaries.

**Acceptance:** a handwritten static C# smoke host can trim and NativeAOT-publish
the providers without unreviewed warnings; byte, random-state, BLOB, socket, and
ownership tests preserve current behavior.

### Phase 2: Compiler static resolution and direct IL

- Resolve imports and contracts from target metadata.
- Emit factory registration and direct managed adapters.
- Make generated modules configure their own transitive dependencies.
- Add static-link diagnostics and versioned deployment metadata.
- Preserve dynamic SRS/function behavior inside the registered import closure.

**Acceptance:** static output contains direct assembly/member references and no
call to the legacy loader; managed coercion and module/provider semantics match
the dynamic path; existing compiler APIs and tests remain compatible.

### Phase 3: SDK trimming and single-file integration

- Wire public properties, target ordering, compiler inputs, and incremental
  fingerprints through `Lolc`.
- Let the SDK own project runtime configuration.
- Emit compatibility metadata and honor portable, embedded, and no-PDB modes.
- Validate packed-package consumers, not only source-tree SDK imports.

**Acceptance:** ordinary, self-contained, single-file, trimmed, and
trimmed-single-file fixtures publish and execute from isolated output without
accidental source, NuGet-cache, or build-directory dependencies.

### Phase 4: NativeAOT and game validation

- Publish final applications through `PublishAot`.
- Mark reusable game libraries independently static-compatible.
- Extend process helpers to execute native binaries.
- Run scripted Galaxy and Catacombs scenarios, including Galaxy save/load.
- Verify the compiler/task assemblies do not enter the application closure.

**Acceptance:** supported RIDs produce executable native applications with no
required managed/configuration sidecars and no unreviewed AOT warnings.

### Phase 5: File-based apps, packaging, CI, and documentation

- Define explicit NativeAOT opt-in/default behavior for file-based apps.
- Document the normal host-project route for API-emitted assemblies.
- Pack static provider contracts and targets.
- Add platform-aware publish smoke jobs for Linux x64, Windows x64, and macOS
  Arm64 where matching runners/toolchains are available.
- Document provider authoring, troubleshooting, external assets, and unsupported
  plugin/native-export scenarios.

**Acceptance:** installed SDK/provider/template consumers pass the deployment
matrix without a source checkout, while existing managed, script, browser, and
conformance paths remain green.

## Principal risks

| Risk | Mitigation |
|---|---|
| Dynamic imports regress | Separate modes and APIs; retain legacy default tests |
| Resource ownership changes | Centralize invocation semantics and reuse ownership tests |
| Providers are trimmed | Emit direct references and lazy factory delegates |
| Unused providers remain rooted | Register only the resolved import closure |
| Old packages appear compatible | Versioned metadata and rebuild diagnostics |
| Build succeeds but publish is broken | Execute relocated final artifacts |
| Mode changes reuse stale IL | Include deployment contracts in compiler fingerprints |
| Tool assemblies enter app publish | Assert host-tool dependency isolation |
| Raw byte output changes | Explicit byte sinks and byte-for-byte tests |
| Warnings are hidden | No blanket suppression; review every retained warning |

## Completion criteria

This work is complete when:

1. Existing managed/JIT, script, browser, and library APIs retain supported
   behavior.
2. Static imports perform no runtime assembly or member discovery.
3. `.lolproj` applications publish through ordinary .NET SDK properties.
4. Providers, managed libraries, and transitive LOLCODE modules work in
   trimmed, single-file, and NativeAOT fixtures.
5. Galaxy and Catacombs run meaningful native scripted sessions on validated
   platforms.
6. Supported publishes contain no unreviewed trim/AOT warnings.
7. Unsupported dynamic/plugin/native-export scenarios fail with explicit
   diagnostics.
8. Packed SDK/provider/template consumers pass the same deployment tests.
9. Documentation distinguishes managed bundles, native executables, and native
   shared libraries accurately.

## References

- [Native AOT deployment](https://learn.microsoft.com/dotnet/core/deploying/native-aot/)
- [Native AOT cross-compilation](https://learn.microsoft.com/dotnet/core/deploying/native-aot/cross-compile)
- [Native libraries with Native AOT](https://learn.microsoft.com/dotnet/core/deploying/native-aot/libraries)
- [Single-file deployment](https://learn.microsoft.com/dotnet/core/deploying/single-file/overview)
- [Trim warnings](https://learn.microsoft.com/dotnet/core/deploying/trimming/fixing-warnings)
- [Preparing libraries for trimming](https://learn.microsoft.com/dotnet/core/deploying/trimming/prepare-libraries-for-trimming)
- [`PersistedAssemblyBuilder`](https://learn.microsoft.com/dotnet/api/system.reflection.emit.persistedassemblybuilder?view=net-10.0)
- [Extending the MSBuild build process](https://learn.microsoft.com/visualstudio/msbuild/how-to-extend-the-visual-studio-build-process)
