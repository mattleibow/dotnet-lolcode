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

The SDK adds the four official providers by default. Set
`LolcodeUseDefaultLibraries=false` to opt out, or give
`LolcodeRuntimePackageVersion` a version to replace the SDK's default provider
version. An explicit `PackageReference` to a provider wins over the implicit
one. `LolcodeUseSourceLibraries=true` removes package defaults so a source
checkout can use project references and matching descriptors instead.

## File or project?

Use `dotnet run --file hello.lol` for an app with a shebang and `#:sdk`
directive. That workflow resolves the published SDK named in the directive.
Use a `.lolproj` for references, explicit file ordering, a library output, or
publish. In this repository, `samples/Directory.Build.props` redirects both
file-based and `.lolproj` samples to source-built task binaries and source
provider projects. Build the solution first; it deliberately does not silently
fall back to a package compiler.

The source tree builds as `0.3.0-local`; the file samples deliberately name
the published `Lolcode.NET.Sdk@0.2.0`. They are different availability facts,
not interchangeable promises.
