# .NET projects

<span class="badge badge-dotnet">.NET workflow</span>

LOLCODE projects use the .NET SDK, so `dotnet build`, project references,
publish, and normal output folders still apply. A `.lolproj` imports
`Lolcode.NET.Sdk` instead of `Microsoft.NET.Sdk` directly; the LOLCODE SDK then
imports the base SDK and replaces its compile step.

```xml
<Project Sdk="Lolcode.NET.Sdk/0.2.0">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

`0.2.0` is the latest published SDK and supports the basic project workflow
above. This section also documents the repository's `0.3.0` source features,
including multi-file interop and modular providers. Use a source checkout or a
locally packed feed for those features until `0.3.0` is published. See
[versions and support](../language/versions.md).

## Choose a project task

- [Multiple source files](multiple-files.md) explains compilation units and
  initialization order.
- [Class libraries](class-libraries.md) exposes LOLCODE functions to C# and
  another LOLCODE project.
- [Managed imports](managed-imports.md) calls a regular C# library with
  `CAN HAS`.
- [Runtime providers](providers.md) configures `STRING`, `STDLIB`, `STDIO`,
  and `SOCKS`.
- [SDK reference](sdk-reference.md) lists the props, targets, task inputs, and
  source-checkout behavior.
- [Publishing](publishing.md) covers deployment artifacts and runtime files.
- [Embedding and scripting](embedding.md) covers syntax, diagnostics, stream
  emission, path emission, and in-memory execution.

For a single `.lol` file without a project, use the
[file-based workflow](../getting-started/file-based.md).
