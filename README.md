# 🐱 dotnet-lolcode

[![CI](https://github.com/mattleibow/dotnet-lolcode/actions/workflows/ci.yml/badge.svg)](https://github.com/mattleibow/dotnet-lolcode/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/mattleibow/dotnet-lolcode/branch/main/graph/badge.svg)](https://codecov.io/gh/mattleibow/dotnet-lolcode)

A **LOLCODE 1.2 compiler** written in C# 14 that compiles `.lol` source files to valid .NET IL assemblies, runnable with `dotnet`.

> HAI 1.2
> VISIBLE "OH HAI! I CAN HAZ COMPILER!"
> KTHXBYE

## What Is This?

This project is a from-scratch compiler for the [LOLCODE](http://www.lolcode.org/) esoteric programming language, targeting the .NET 10 runtime. It uses a **hand-rolled, Roslyn-inspired architecture** — no parser generators, no transpiling to C# — just raw lexer → parser → binder → IL emitter producing real .NET assemblies.

## Architecture

```
┌─────────────┐    ┌─────────────┐    ┌──────────────┐    ┌──────────────┐    ┌─────────────┐
│  Source      │    │   Lexer     │    │    Parser    │    │   Binder     │    │  IL Emitter  │
│  (.lol file) │───▶│  (Tokenizer)│───▶│  (AST Build) │───▶│  (Semantics) │───▶│  (DLL out)   │
└─────────────┘    └─────────────┘    └──────────────┘    └──────────────┘    └─────────────┘
```

| Stage | What It Does |
|-------|-------------|
| **Lexer** | Scans source text into tokens (`VISIBLE`, `HAI`, `42`, `"YARN"`, ...) |
| **Parser** | Recursive descent parser builds an Abstract Syntax Tree (AST) |
| **Binder** | Resolves types, validates semantics, produces a bound tree with diagnostics |
| **Emitter** | Walks the bound tree, emits CIL opcodes via `PersistedAssemblyBuilder`, saves `.dll` |

## Features

- 🐱 **LOLCODE 1.2** — variables, types, math, conditionals, loops, functions, casting, string ops, TYPE
- 🎯 **Compiles to .NET IL** — produces real .NET assemblies (not interpreted)
- 🔧 **CLI tool** — `lolcode compile`, `lolcode run`
- 🎨 **VS Code extension** — syntax highlighting, snippets, build tasks
- 📦 **MSBuild SDK** — `dotnet build` / `dotnet run` integration for `.lol` files
- 🐛 **Debugging** — (planned) VS Code debugging via Debug Adapter Protocol

## Quick Start

```bash
# Install the CLI tool
dotnet tool install -g lolcode

# Compile a LOLCODE program
lolcode compile hello.lol

# Run it
dotnet hello.dll

# Or compile and run in one step
lolcode run hello.lol
```

## Example: Hello World

```lolcode
HAI 1.2
  VISIBLE "HAI WORLD!"
KTHXBYE
```

## Example: FizzBuzz

```lolcode
HAI 1.2
  I HAS A i ITZ 1
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

## Project Structure

```
dotnet-lolcode/
├── src/
│   ├── Lolcode.Compiler/     # Core compiler (lexer, parser, binder, emitter)
│   ├── Lolcode.Runtime/       # Runtime helper library (referenced by compiled programs)
│   ├── Lolcode.Cli/          # CLI tool
│   └── Lolcode.Sdk/          # MSBuild SDK for dotnet build integration
├── tests/
│   └── Lolcode.Compiler.Tests/
├── samples/                   # 15 example programs (graduated complexity)
├── editor/
│   └── vscode-lolcode/       # VS Code extension
└── docs/                      # Design documents
```

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
- **Parser:** Hand-rolled recursive descent (Roslyn-inspired)
- **VS Code:** TextMate grammar + Language Server Protocol
- **Testing:** xUnit + FluentAssertions

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
