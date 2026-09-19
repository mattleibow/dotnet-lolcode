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

The published `0.2.0` examples are intentionally small. Features documented
here for providers, managed imports, and library exports describe the
source-checkout development revision (`0.3.0-local`) unless a package release
explicitly includes them. See [versions and support](../language/versions.md)
before selecting a package version.

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

For a single `.lol` file without a project, use the
[file-based workflow](../getting-started/file-based.md).
