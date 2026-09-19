# Publishing and deployment

<span class="badge badge-dotnet">Deployment</span>

Project-based applications use the standard .NET publish pipeline after
`Lolcode.NET.Sdk` has produced the managed assembly:

```bash
dotnet publish MyApp.lolproj --configuration Release --output publish
dotnet publish MyApp.lolproj --runtime linux-x64 --self-contained false
```

Deploy the complete publish directory, not only the application DLL. The
generated executable needs its `.runtimeconfig.json`, `.deps.json`, the
`Lolcode.Runtime.dll` copied by the SDK, and any provider or managed-library
assemblies that it imports. Project references arrange these files through
normal .NET resolution; a manually copied `CAN HAS` library must still sit in
the application's base directory under its simple assembly name.

`LolcodeCompilation.Emit` writes path-based outputs transactionally and emits a
portable PDB when source file paths are available. Executable output includes
runtime configuration; library output has no entry point and does not retain an
executable runtime configuration. The compiler course covers the details in
[assemblies, scripts, and tooling](../compiler-course/tooling.md).

File-based apps are best for a local, small program. For repeatable deployment,
versioning, references, and provider selection, convert the source to a
`.lolproj` and publish that project. Package availability remains independent
of language support: verify the SDK and provider versions described in
[versions and support](../language/versions.md).
