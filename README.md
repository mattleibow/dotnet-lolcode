# 🐱 dotnet-lolcode

A **LOLCODE 1.2 compiler** written in C# 14 that compiles `.lol` source files to valid .NET IL assemblies, runnable with `dotnet`.

> HAI 1.2
> VISIBLE "OH HAI! I CAN HAZ COMPILER!"
> KTHXBYE

## What Is This?

This project is a from-scratch compiler for the [LOLCODE](http://www.lolcode.org/) esoteric programming language, targeting the .NET 10 runtime. It uses a **hand-rolled, Roslyn-inspired architecture** — no parser generators, no transpiling to C# — just raw lexer → parser → binder → lowerer → code generator producing real .NET assemblies.

## Architecture

```
┌─────────────┐    ┌─────────────┐    ┌──────────────┐    ┌──────────────┐    ┌──────────────┐    ┌────────────────┐
│  Source      │    │   Lexer     │    │    Parser    │    │   Binder     │    │   Lowerer    │    │ Code Generator │
│  (.lol file) │───▶│  (Tokenizer)│───▶│  (AST Build) │───▶│  (Semantics) │───▶│  (Desugar)   │───▶│  (DLL out)     │
└─────────────┘    └─────────────┘    └──────────────┘    └──────────────┘    └──────────────┘    └────────────────┘
```

| Stage | What It Does |
|-------|-------------|
| **Lexer** | Scans source text into tokens (`VISIBLE`, `HAI`, `42`, `"YARN"`, ...) |
| **Parser** | Recursive descent parser builds an Abstract Syntax Tree (AST) |
| **Binder** | Resolves types, validates semantics, produces a bound tree with diagnostics |
| **Lowerer** | Desugars complex bound nodes into simpler forms for emission |
| **Code Generator** | Walks the lowered tree, emits CIL opcodes via `PersistedAssemblyBuilder`, saves `.dll` |

## Compiler API

```csharp
// Parse source code
var tree = SyntaxTree.ParseText("HAI 1.2\nVISIBLE \"HAI WORLD!\"\nKTHXBYE");

// Create compilation and emit
var compilation = LolcodeCompilation.Create(tree);
var result = compilation.Emit("output.dll", runtimeAssemblyPath);

if (!result.Success)
    foreach (var d in result.Diagnostics)
        Console.Error.WriteLine(d);
```

## Features

- 🐱 **Full LOLCODE 1.2** — variables, types, math, booleans, conditionals, loops, functions, casting, string ops
- 🎯 **Compiles to .NET IL** — produces real .NET assemblies (not interpreted)
- 📦 **MSBuild SDK** — `dotnet build` and `dotnet run` for `.lolproj` projects
- 🚀 **File-based apps** — `dotnet run --file hello.lol` with no project needed
- 📊 **Pretty diagnostics** — error messages with source context and line/column info
- 🧪 **343 tests** — unit tests + conformance test suite (116 `.lol`/`.txt` test pairs)

## Quick Start

### File-based (no project needed)

```bash
# Create a LOLCODE file with the SDK directive
cat > hello.lol << 'EOF'
#:sdk Lolcode.NET.Sdk
HAI 1.2
  VISIBLE "HAI WORLD!"
KTHXBYE
EOF

# Run it directly
dotnet run --file hello.lol
# Or even shorter:
dotnet hello.lol
```

### Project-based (for real apps)

```bash
# Create a new LOLCODE project
dotnet new lolcode -n MyApp
cd MyApp

# Build and run
dotnet build
dotnet run
```

### From source

```bash
# Clone and build the compiler
git clone https://github.com/mattleibow/dotnet-lolcode.git
cd dotnet-lolcode
dotnet build

# Run tests
dotnet test
```

## MSBuild SDK (.lolproj)

Build LOLCODE projects with standard .NET tooling — no CLI required:

```xml
<!-- MyApp.lolproj -->
<Project Sdk="Lolcode.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

```bash
dotnet build    # Compiles .lol files → .dll
dotnet run      # Compile and execute
dotnet publish  # Publish for deployment
dotnet watch    # Recompile on .lol file changes
```

Create a new project from template:
```bash
dotnet new install Lolcode.NET.Templates
dotnet new lolcode -n MyApp
cd MyApp && dotnet run
```

See [samples/sdk-hello-world](samples/sdk-hello-world/) for a complete example.

## Example: Hello World

```lolcode
HAI 1.2
  VISIBLE "HAI WORLD!"
KTHXBYE
```

## Example: FizzBuzz

```lolcode
HAI 1.2
  IM IN YR fizzbuzz UPPIN YR i TIL BOTH SAEM i AN 101
    I HAS A out ITZ ""
    BOTH SAEM MOD OF i AN 3 AN 0, O RLY?
      YA RLY, out R "Fizz"
    OIC
    BOTH SAEM MOD OF i AN 5 AN 0, O RLY?
      YA RLY, out R SMOOSH out AN "Buzz" MKAY
    OIC
    BOTH SAEM out AN "", O RLY?
      YA RLY, VISIBLE i
      NO WAI, VISIBLE out
    OIC
  IM OUTTA YR fizzbuzz
KTHXBYE
```

## Example: Recursive Factorial

```lolcode
HAI 1.2
  HOW IZ I factorial YR n
    BOTH SAEM n AN 0
    O RLY?
      YA RLY
        FOUND YR 1
    OIC
    FOUND YR PRODUKT OF n AN I IZ factorial YR DIFF OF n AN 1 MKAY
  IF U SAY SO

  VISIBLE I IZ factorial YR 10 MKAY  BTW prints 3628800
KTHXBYE
```

## Project Structure

```
dotnet-lolcode/
├── src/
│   ├── Lolcode.CodeAnalysis/     # Core compiler (lexer, parser, binder, lowerer, code generator)
│   ├── Lolcode.Runtime/          # Runtime helper library
│   ├── Lolcode.Build/            # MSBuild task (Lolc) for SDK integration
│   ├── Lolcode.NET.Sdk/          # MSBuild SDK package (Sdk.props, Sdk.targets)
│   └── Lolcode.NET.Templates/    # dotnet new template pack
├── tests/
│   ├── Lolcode.CodeAnalysis.Tests/ # Unit + end-to-end + conformance tests
│   └── ...                       # 18 test categories, 117 test pairs
├── samples/                      # Example programs + SDK samples
└── docs/                         # Design documents and language spec
```

## Running Tests

```bash
# Run all 343 tests
dotnet test

# Run specific test category
dotnet test --filter "EndToEndTests"
dotnet test --filter "ConformanceTests"
```

## Supported Language Features

| Feature | Syntax | Status |
|---------|--------|--------|
| Variables | `I HAS A x ITZ 42` | ✅ |
| Assignment | `x R 100` | ✅ |
| NUMBR (int) | `42`, `-7` | ✅ |
| NUMBAR (float) | `3.14` | ✅ |
| YARN (string) | `"hello"` with escapes | ✅ |
| TROOF (bool) | `WIN`, `FAIL` | ✅ |
| NOOB (null) | uninitialized variables | ✅ |
| Print | `VISIBLE "text"` | ✅ |
| Input | `GIMMEH var` | ✅ |
| Math | `SUM OF`, `DIFF OF`, `PRODUKT OF`, `QUOSHUNT OF`, `MOD OF`, `BIGGR OF`, `SMALLR OF` | ✅ |
| Comparison | `BOTH SAEM`, `DIFFRINT` | ✅ |
| Boolean | `BOTH OF`, `EITHER OF`, `WON OF`, `NOT`, `ALL OF`, `ANY OF` | ✅ |
| Conditionals | `O RLY?`, `YA RLY`, `MEBBE`, `NO WAI`, `OIC` | ✅ |
| Switch | `WTF?`, `OMG`, `OMGWTF`, `OIC` (with fall-through) | ✅ |
| Loops | `IM IN YR`, `UPPIN`, `NERFIN`, `TIL`, `WILE`, `GTFO` | ✅ |
| Functions | `HOW IZ I`, `IF U SAY SO`, `FOUND YR`, `I IZ func MKAY` | ✅ |
| Casting | `MAEK x A NUMBR`, `x IS NOW A YARN` | ✅ |
| Strings | `SMOOSH`, string interpolation `:{var}`, escape sequences | ✅ |
| Comments | `BTW` (line), `OBTW...TLDR` (block) | ✅ |
| IT variable | Implicit per-scope variable | ✅ |
| Line continuation | `...` and `…` | ✅ |
| TYPE type | Bare word type values | 🚧 Deferred |

## Documentation

| Document | Description |
|----------|-------------|
| [Design Document](docs/DESIGN.md) | Architecture, technical decisions, component details |
| [Language Specification](docs/LANGUAGE_SPEC.md) | Full LOLCODE 1.2 spec as implemented |
| [Roadmap](docs/ROADMAP.md) | Build phases and progress tracking |
| [Contributing](CONTRIBUTING.md) | How to contribute |

## Technology

- **Runtime:** .NET 10 / C# 14
- **IL Emission:** `System.Reflection.Emit.PersistedAssemblyBuilder`
- **Parser:** Hand-rolled recursive descent (Roslyn-inspired architecture)
- **Testing:** xUnit + FluentAssertions
- **CI:** GitHub Actions (Ubuntu, macOS, Windows)

## Sample Programs

| # | Sample | Concepts |
|---|--------|----------|
| 01 | [Hello World](samples/01-hello-world/hello.lol) | Program structure, VISIBLE |
| 02 | [Variables](samples/02-variables/variables.lol) | I HAS A, ITZ, R, types |
| 03 | [Math](samples/03-math/math.lol) | SUM OF, DIFF OF, PRODUKT OF, etc. |
| 04 | [Conditionals](samples/04-conditionals/conditionals.lol) | O RLY?, YA RLY, NO WAI, MEBBE |
| 05 | [Loops](samples/05-loops/loops.lol) | IM IN YR, UPPIN, NERFIN, TIL, WILE |
| 06 | [Functions](samples/06-functions/functions.lol) | HOW IZ I, FOUND YR, IF U SAY SO |
| 07 | [String Ops](samples/07-string-ops/strings.lol) | SMOOSH, string escapes |
| 08 | [Casting](samples/08-casting/casting.lol) | MAEK, IS NOW A |
| 09 | [Switch](samples/09-switch/switch.lol) | WTF?, OMG, OMGWTF |
| 10 | [FizzBuzz](samples/10-fizzbuzz/fizzbuzz.lol) | Combined: loops + conditionals + math |
| 11 | [Fibonacci](samples/11-fibonacci/fibonacci.lol) | Functions + recursion |
| 12 | [Guessing Game](samples/12-guessing-game/guess.lol) | I/O + loops + conditionals |
| 13 | [Recursion](samples/13-recursion/recursion.lol) | Recursive functions |
| 14 | [Calculator](samples/14-string-calculator/calculator.lol) | Parsing + switch + functions |
| 15 | [Adventure Game](samples/15-adventure-game/adventure.lol) | Full program: I/O, state, functions |

## License

[MIT](LICENSE)
