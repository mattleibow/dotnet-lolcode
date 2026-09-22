# 🐱 Lolcode.NET.Sdk

MSBuild SDK for compiling **LOLCODE 1.2** programs to .NET assemblies. Write `.lol` files, build with `dotnet build`, run with `dotnet run`.

## Quick Start

### Create a project from template

```bash
dotnet new install Lolcode.NET.Templates
dotnet new lolconsole -n MyApp
cd MyApp
dotnet run
```

### Manual project setup

Create a `MyApp.lolproj`:

```xml
<Project Sdk="Lolcode.NET.Sdk/0.2.0">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

Create a `Program.lol`:

```
HAI 1.2
  VISIBLE "HAI WORLD!"
KTHXBYE
```

Build and run:

```bash
dotnet build    # Compiles .lol → .dll
dotnet run      # Compile and execute
dotnet watch    # Recompile on changes
dotnet publish  # Publish for deployment
```

### Class libraries

Set `OutputType` to `Library` to emit a DLL with no entry point or runtime
configuration file. Top-level directly named `HOW IZ I` functions become public
static methods returning and accepting `object`, so C# projects can consume the
library through a normal `ProjectReference`.

```xml
<PropertyGroup>
  <OutputType>Library</OutputType>
  <TargetFramework>net10.0</TargetFramework>
  <RootNamespace>InteropSamples</RootNamespace>
  <LolcodeLibraryTypeName>LolcatExports</LolcodeLibraryTypeName>
</PropertyGroup>
```

`LolcodeLibraryTypeName` must be a simple type name (without dots). When it is
omitted, the SDK derives a valid CLR identifier from `AssemblyName` (for
example, `Lolcat-Phrase` becomes `Lolcat_Phrase`). The SDK composes the emitted
CLR type as `$(RootNamespace).$(LolcodeLibraryTypeName)`. Set `RootNamespace`
explicitly to an empty value (including with `-p:RootNamespace=`) to emit the
type in the global namespace. Otherwise, the .NET SDK default derived from
`AssemblyName` is used. Every namespace segment is normalized to a CLR
identifier, making a hyphenated or leading-digit default safe for C# consumers.

For multi-file projects, direct top-level functions are installed before normal
top-level initialization regardless of `Compile` order. In 1.3/1.4, calls
continue to dynamically resolve the current function slot, so later
replacements remain effective. A class-library wrapper initializer evaluates
only supported import and declaration forms, never arbitrary top-level
executable statements.

### File-based apps (no project needed)

Create `hello.lol` — no project file required:

```
#:sdk Lolcode.NET.Sdk@0.2.0
HAI 1.2
  VISIBLE "HAI WORLD!"
KTHXBYE
```

```bash
dotnet run --file hello.lol
```

## Language Features

Full LOLCODE 1.2 support:

```
HAI 1.2
  BTW Variables and types
  I HAS A name ITZ "LOLCODE"
  I HAS A count ITZ 42
  I HAS A pi ITZ 3.14
  I HAS A cool ITZ WIN

  BTW Math
  VISIBLE SUM OF count AN 8          BTW 50
  VISIBLE PRODUKT OF count AN 2      BTW 84

  BTW String concatenation
  VISIBLE SMOOSH "HAI " AN name AN "!" MKAY

  BTW Conditionals
  BOTH SAEM count AN 42, O RLY?
    YA RLY, VISIBLE "IT IZ 42!"
    NO WAI, VISIBLE "IT IZ NOT 42"
  OIC

  BTW Loops
  IM IN YR loop UPPIN YR i TIL BOTH SAEM i AN 5
    VISIBLE SMOOSH "COUNT: " AN i MKAY
  IM OUTTA YR loop

  BTW Functions
  HOW IZ I greet YR who
    FOUND YR SMOOSH "OH HAI " AN who AN "!" MKAY
  IF U SAY SO

  VISIBLE I IZ greet YR "WORLD" MKAY
KTHXBYE
```

## What's Included

The SDK package contains:
- **LOLCODE compiler** — full lexer → parser → binder → lowerer → code generator pipeline
- **MSBuild integration** — `Sdk.props` and `Sdk.targets` for seamless `dotnet` CLI experience
- **Runtime library and providers** — `Lolcode.Runtime.dll` plus the bundled
  `STRING`, `STDLIB`, `STDIO`, and `SOCKS` provider assemblies are private
  references, not consumer NuGet dependencies

## `CAN HAS` providers

The built-in providers are SDK-supplied build/runtime assets. `CAN HAS STRING?`
(and the other built-in names) loads a provider only when the program imports
it; merely compiling an application does not initialize provider code. Normal
build and publish include the provider assemblies today. Publish trimming of
unused providers is a future optimization.

For a custom managed import, reference the assembly normally and use its
assembly name in `CAN HAS`. The runtime resolves the copied assembly using the
existing convention and selects a suitable static export type. Aliases are not
supported; the four reserved built-in names cannot be replaced.

## Requirements

- .NET 10 SDK

## Links

- [GitHub Repository](https://github.com/mattleibow/dotnet-lolcode)
- [Language Specification](https://github.com/mattleibow/dotnet-lolcode/blob/main/docs/LANGUAGE_SPEC.md)
- [Sample Programs](https://github.com/mattleibow/dotnet-lolcode/tree/main/samples)

## License

[MIT](https://github.com/mattleibow/dotnet-lolcode/blob/main/LICENSE)
