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
runtime, and resolved assets. Framework-dependent publish copies the four
SDK-bundled libraries through normal .NET assets. Project references arrange
custom module files the same way; a deliberately external managed `CAN HAS`
assembly remains beside the host under its simple assembly name.

`PublishSingleFile` is a .NET bundle, not a merged assembly. SDK-bundled
libraries load through the default runtime context. Deliberate dynamic managed
DLL imports remain external beside the host. Provider trimming is deferred;
dynamic managed imports are unsupported with trimming and NativeAOT.

`LolcodeCompilation.Emit` path output coordinates DLL, optional PDB, and
runtime configuration as one path operation. If optional PDB staging or
serialization fails, it can still emit PE without symbols. Stream emission
creates no files, accepts caller-owned writable PE/PDB streams, needs no
runtime DLL path, and propagates PDB write failures. Neither API wildcard-copies
provider DLLs; deployment belongs to SDK and resolved assets. The compiler
course covers the details in [assemblies, scripts, and tooling](../compiler-course/tooling.md).

File-based apps are best for a local, small program. For repeatable deployment,
versioning, references, and custom modules, convert the source to a `.lolproj`
and publish that project. Package availability remains independent of language
support: verify the SDK version described in
[versions and support](../language/versions.md).
