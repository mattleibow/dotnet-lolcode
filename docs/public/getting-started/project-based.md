# Project-based apps

Use a `.lolproj` when source files, target settings, or publishing deserve a
home. Install the template package, create a project, and run it:

```bash
dotnet new install Lolcode.NET.Templates
dotnet new lolconsole -n MyApp
cd MyApp
dotnet run
```

The project uses the `Lolcode.NET.Sdk` SDK and therefore participates in the
usual `dotnet build`, `dotnet run`, `dotnet test`, and `dotnet publish` flow.
The source-tree example is
[`samples/project-based/hello-world/Program.lol`](../../../samples/project-based/hello-world/Program.lol).

```xml
<Project Sdk="Lolcode.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

The SDK globs `.lol` files, invokes the compiler task, and references
`Lolcode.Runtime`. See [tooling](tooling.md) for editor habits and the
[compiler course tooling chapter](../compiler-course/tooling.md) to see how
that integration works.
