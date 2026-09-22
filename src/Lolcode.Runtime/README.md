# 🐱 Lolcode.Runtime

Runtime support library for compiled **LOLCODE** programs. This package is automatically referenced when building `.lolproj` projects with the `Lolcode.NET.Sdk`.

## What This Package Does

When the LOLCODE compiler generates .NET assemblies from your `.lol` source files, the compiled code calls into this runtime library for:

- **Type coercion** — casting between LOLCODE types (NUMBR, NUMBAR, YARN, TROOF, NOOB)
- **Arithmetic** — type-aware `SUM OF`, `DIFF OF`, `PRODUKT OF`, `QUOSHUNT OF`, `MOD OF` with NUMBR/NUMBAR promotion
- **Comparison** — `BOTH SAEM`, `DIFFRINT` with strict no-auto-cast semantics
- **Boolean logic** — truthiness evaluation, `BOTH OF`, `EITHER OF`, `WON OF`, `NOT`
- **String operations** — `SMOOSH` concatenation with auto-YARN casting, NUMBAR 2-decimal formatting
- **I/O** — `VISIBLE` (print with infinite arity and newline suppression) and `GIMMEH` (read input)
- **Min/Max** — `BIGGR OF`, `SMALLR OF` with type-aware comparison

## Usage

You don't need to reference this package directly. It is automatically included when you use the `Lolcode.NET.Sdk`:

```xml
<Project Sdk="Lolcode.NET.Sdk/0.2.0">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

```
HAI 1.2
  VISIBLE "I CAN HAZ RUNTIME!"
KTHXBYE
```

```bash
dotnet run    # The runtime is automatically available
```

## Library providers

`Lolcode.Runtime` remains the shared value, scope, BUKKIT, invocation, resource,
and managed-library loading runtime. Official `CAN HAS` libraries are SDK-bundled BCL-like facilities:
`STRING`, `STDLIB`, `STDIO`, and `SOCKS`. They are not separately installed
consumer packages. The SDK supplies their assemblies privately, and `CAN HAS`
is the runtime opt-in/import operation: registration does not load a provider
until it is imported.

Custom referenced providers use the public assembly-level
`LolcodeLibraryProviderAttribute`. A normal `ProjectReference` or `Reference`
is sufficient; the compiler discovers metadata only, validates the contract,
and embeds the registration needed at runtime. Reserved built-in names cannot
be overridden.

Registered providers use the same typed managed invocation path as ordinary
managed assemblies. They may receive a first, exact `LolcodeLibraryContext`
parameter for scope-bound BLOB cleanup or per-import state; no mutable global
runtime context is used. Ordinary plugins cannot receive that parameter.

## Publishing

Normal framework-dependent publishing copies all SDK-bundled provider
assemblies with the host, including when that host reaches a LOLCODE class
library through a normal C# `ProjectReference`. `PublishSingleFile` produces an
executable bundle rather than a merged DLL; on current .NET SDKs it is
self-contained. Provider trimming is deliberately deferred, but registration
does not statically reference provider types so a future trimming policy can
remove unused providers. Deliberately external plugins must remain adjacent to
the host.

Dynamic managed libraries are not supported with trimming or NativeAOT.

## Multiple source files

A compilation can contain multiple complete `.lol` files. The compiler discovers
top-level declarations across the complete source set. In a multi-file project,
direct top-level functions are installed before other top-level initialization,
so their file order does not matter even though 1.3/1.4 calls still resolve the
current replaceable function value at runtime. Dynamic/SRS declarations,
variables, imports, and other initialization remain ordered. Single-file
1.3/1.4 programs retain textual declaration behavior. Library wrapper
initialization runs only supported import and declaration forms; it does not run
arbitrary top-level executable statements.

## Requirements

- .NET 10

## Links

- [GitHub Repository](https://github.com/mattleibow/dotnet-lolcode)
- [Lolcode.NET.Sdk](https://www.nuget.org/packages/Lolcode.NET.Sdk) — the compiler SDK package

## License

[MIT](https://github.com/mattleibow/dotnet-lolcode/blob/main/LICENSE)
