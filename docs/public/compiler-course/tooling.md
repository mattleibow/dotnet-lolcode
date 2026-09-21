# Assemblies, scripts, MSBuild, templates, and file-based apps

Path-based emission is useful for `dotnet run`; browser and host integrations
also need in-memory execution. `Scripting/LolcodeScript.cs` exposes
`LolcodeScript.Create`, `Compile`, and `Run`. It caches emitted bytes, captures
input/output through `LolcodeScriptExecutionOptions`, and loads assemblies
collectibly on CoreCLR where possible. `LolcodeScriptState` reports diagnostics,
captured streams, return values, and runtime exceptions.

The lower-level stream `Emit` overload accepts caller-owned writable PE and
optional PDB streams, creates no files, needs no runtime path, and propagates
PDB write failures. Path `Emit` stages coordinated PE/PDB/runtimeconfig output;
optional symbol failure can produce a valid PE without symbols. A zero-tree
executable is valid. Direct API consumers deploy resolved runtime/provider
assets explicitly.

The compiler becomes practical through ordinary .NET integration:

- `src/Lolcode.Build` contains the `Lolc` MSBuild task.
- `src/Lolcode.NET.Sdk/Sdk.props` and `Sdk.targets` gather `.lol` files and
  replace the normal compile path.
- `src/Lolcode.NET.Templates` offers `dotnet new lolconsole`.
- `samples/project-based/hello-world` exercises the project route.
- `samples/Directory.Build.props` makes source-tree file-based samples use the
  built compiler, while `#:sdk` enables `dotnet run --file`.

The task parses every selected `.lol` file into one compilation, passes MSBuild
reference paths and provider descriptors to emission, and honors `OutputType`
for executables or public CLR library wrappers. `Sdk.targets` exposes
incremental inputs and `SkipCompilerExecution` for design-time builds. For
provider package choices, explicit compile order, source-checkout behavior, and
deployment files, see [.NET projects](../projects/index.md).

File-based execution suppresses the project `**/*.lol` glob so adjacent source
does not join accidentally. In a project, the ordered `Compile` list is both a
semantic initialization input and an incremental build input; edits, adds,
deletes, or reordering rebuild one assembly.

**Checkpoint:** Run the project sample, then run
`dotnet run --file samples/basics/hello-world/hello.lol`. Compare this with the
[getting-started workflows](../getting-started/index.md). For API consumers, see
the [generated API reference](xref:Lolcode.CodeAnalysis).
