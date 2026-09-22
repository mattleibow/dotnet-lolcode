# Publishing and deployment

<span class="badge badge-dotnet">Deployment</span>

Project-based applications use the standard .NET publish pipeline after
`Lolcode.NET.Sdk` has produced the managed assembly:

```bash
dotnet publish MyApp.lolproj --configuration Release --output publish
dotnet publish MyApp.lolproj --runtime linux-x64 --self-contained false
```

Deploy the complete publish directory, not only the application DLL. The
generated executable needs its normal `.runtimeconfig.json`, `.deps.json`,
runtime, and resolved assets. Framework-dependent publish copies the single
SDK-bundled runtime DLL through normal .NET assets. Project references arrange
referenced managed library assemblies the same way.

`PublishSingleFile` is a .NET bundle, not a merged assembly. The runtime loads
through the default runtime context. Trimming is deferred because
reflection-based library discovery needs an explicit future rooting policy.

`LolcodeCompilation.Emit` path output coordinates DLL, optional PDB, and
runtime configuration as one path operation. If optional PDB staging or
serialization fails, it can still emit PE without symbols. Stream emission
creates no files, accepts caller-owned writable PE/PDB streams, needs no
runtime DLL path, and propagates PDB write failures. Neither API deploys reference assets; deployment belongs to SDK and
resolved assets. The compiler
course covers the details in [assemblies, scripts, and tooling](../compiler-course/tooling.md).

File-based apps are best for a local, small program. For repeatable deployment,
versioning, references, and managed libraries, convert the source to a `.lolproj`
and publish that project. Package availability remains independent of language
support: verify the SDK version described in
[versions and support](../language/versions.md).
