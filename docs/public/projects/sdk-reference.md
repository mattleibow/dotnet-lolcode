# SDK and task reference

<span class="badge badge-dotnet">MSBuild</span>

`Lolcode.NET.Sdk` has a conventional two-part SDK shape:

| Component | Responsibility |
| --- | --- |
| `Sdk.props` | Imports `Microsoft.NET.Sdk` props, sets `Language=LOLCODE`, disables C#-specific generated source, globs `**/*.lol`, and declares default provider packages. |
| `Sdk.targets` | Imports base targets, configures providers, references `Lolcode.Runtime`, creates library export metadata, and replaces `CoreCompile` with `Lolcode.Build.Lolc`. |
| `Lolc` task | Parses every selected source in item order, creates one compilation, passes reference paths, output type, export type name, and provider descriptors to `Emit`. |

`CoreCompile` uses `@(Compile)`, additional inputs, resolved reference
assemblies, imported project files, and the generated compiler-options cache
as inputs. Its output is `$(IntermediateAssembly)`. This makes normal MSBuild
incremental builds work without pretending that individual LOLCODE files are
independent assemblies. `SkipCompilerExecution=true` makes design-time builds
collect metadata without compilation.

## Provider package controls

The SDK adds the four official providers as implicit `PackageReference` items.
Their default version follows `LolcodeRuntimePackageVersion`. A project can use
`PackageReference Update="..."` to change version/metadata or an explicit
provider reference to replace the implicit item. `LolcodeUseDefaultLibraries=false`
and `LolcodeUseSourceLibraries=true` remove only SDK-marked implicit defaults;
they do not remove explicit project references.

## File or project?

Use `dotnet run --file hello.lol` for an app with a shebang and `#:sdk`
directive. That workflow resolves the SDK named in the directive and
suppresses the project `**/*.lol` glob, so adjacent files do not join.
Use a `.lolproj` for references, explicit file ordering, a library output, or
publish. In this repository, `samples/Directory.Build.props` redirects both
file-based and `.lolproj` samples to source-built task binaries and source
provider projects. Build the solution first; it deliberately does not silently
fall back to a package compiler.

The repository `VersionPrefix` is `0.3.0`, but the latest published SDK is
`0.2.0`. Consumer and file-based quick starts use the published version. The
advanced behavior on this page is validated from the source revision and
requires a source checkout or locally packed feed until `0.3.0` is published.
