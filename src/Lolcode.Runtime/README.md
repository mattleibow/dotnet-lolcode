# 🐱 Lolcode.Runtime

Runtime support library for compiled **LOLCODE** programs. This package is automatically referenced when building `.lolproj` projects with the `Lolcode.NET.Sdk`.

The usage example uses the latest published `0.2.0` SDK. The repository's
modular provider and interop work is on the unpublished `0.3.0` source line.

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

## LOLCODE libraries

`Lolcode.Runtime` supplies the shared values, scopes, BUKKITs, invocation and
resource lifetime support. `STRING`, `STDLIB`, `STDIO`, and `SOCKS` are public
sealed, explicitly opted-in LOLCODE library types in this one assembly.

Each importable library has one type-level declaration:

```csharp
[LolcodeLibrary("COUNTER")]
public sealed class CounterLibrary : IDisposable
{
    private int counter;
    public int NEXT() => ++counter;
    public void Dispose() { }
}
```

The compiler reads reference metadata without executing target code. `CAN HAS
COUNTER?` constructs one library instance, projects its direct public instance
methods into one library BUKKIT, and binds that BUKKIT in the importing scope.
A repeated import in that scope is a no-op; separate scopes receive isolated
instances. There is no filename fallback or unaware-DLL import.

When a library implements `IDisposable`, the importing scope disposes it.
Returned open `LolBlob` values are automatically adopted by the calling scope;
closed values are not adopted. Direct C# callers retain normal .NET ownership.

## Publishing

Normal framework-dependent publishing copies the one SDK-bundled runtime
assembly with the host, including when that host reaches a LOLCODE class
library through a normal C# `ProjectReference`. `PublishSingleFile` produces an
executable bundle rather than a merged DLL; on current .NET SDKs it is
self-contained. Trimming is deliberately deferred because reflection-based library discovery
needs an explicit future rooting policy.

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
