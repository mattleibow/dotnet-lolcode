# SDK and task reference

<span class="badge badge-dotnet">MSBuild</span>

`Lolcode.NET.Sdk` has a conventional two-part SDK shape:

| Component | Responsibility |
| --- | --- |
| `Sdk.props` | Imports `Microsoft.NET.Sdk` props, sets `Language=LOLCODE`, disables C#-specific generated source, and globs `**/*.lol`. |
| `Sdk.targets` | Imports base targets, adds the single runtime assembly as a private reference, prepares generated library names, and replaces `CoreCompile` with `Lolcode.Build.Lolc`. |
| `Lolc` task | Parses every selected source in item order, creates one compilation, and passes resolved references, output type, and library type/name settings to `Emit`. |

`CoreCompile` uses `@(Compile)`, additional inputs, resolved reference
assemblies, imported project files, and the generated compiler-options cache
as inputs. Its output is `$(IntermediateAssembly)`. This makes normal MSBuild
incremental builds work without pretending that individual LOLCODE files are
independent assemblies. `SkipCompilerExecution=true` makes design-time builds
collect metadata without compilation.

## Runtime and generated library properties

`Lolcode.Runtime.dll` is the SDK runtime payload. It contains the attributed
`STRING`, `STDLIB`, `STDIO`, and `SOCKS` library classes, so normal build and
publish include one runtime DLL. No per-library package or MSBuild control is
required.

For `OutputType=Library`, `LolcodeLibraryName` is the direct `CAN HAS`
identifier. It is distinct from `AssemblyName` and
`LolcodeLibraryTypeName`. If omitted, it derives deterministically from
`AssemblyName`; explicitly invalid values fail the build.

## File or project?

Use `dotnet run --file hello.lol` for an app with a shebang and `#:sdk`
directive. That workflow resolves the SDK named in the directive and
suppresses the project `**/*.lol` glob, so adjacent files do not join.
Use a `.lolproj` for references, explicit file ordering, a library output, or
publish. In this repository, `samples/Directory.Build.props` redirects both
file-based and `.lolproj` samples to source-built task and runtime binaries.
Build the solution first; it deliberately does not silently
fall back to a package compiler.

The repository `VersionPrefix` is `0.3.0`, but the latest published SDK is
`0.2.0`. Consumer and file-based quick starts use the published version. The
advanced behavior on this page is validated from the source revision and
requires a source checkout or locally packed feed until `0.3.0` is published.
