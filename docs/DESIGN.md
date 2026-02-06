# Architecture & Technical Design

This document describes the internal architecture of the LOLCODE .NET compiler, the technical decisions behind it, and the key components and APIs used.

## Table of Contents

- [Overview](#overview)
- [Compiler Pipeline](#compiler-pipeline)
- [Component Details](#component-details)
  - [Lexer](#1-lexer-tokenizer)
  - [Parser](#2-parser-ast-construction)
  - [Binder](#3-binder-semantic-analysis)
  - [Lowering](#4-lowering-optional)
  - [Emitter](#5-emitter-il-code-generation)
  - [Driver](#6-driver-cli)
  - [Diagnostics](#7-diagnostics)
- [Type System Mapping](#type-system-mapping)
- [IL Emission Strategy](#il-emission-strategy)
- [MSBuild SDK Integration](#msbuild-sdk-integration)
- [VS Code Extension Architecture](#vs-code-extension-architecture)
- [Key .NET APIs](#key-net-apis)
- [Risks and Mitigations](#risks-and-mitigations)

---

## Overview

The LOLCODE compiler is a **multi-phase, ahead-of-time (AOT) compiler** that transforms LOLCODE 1.2 source files (`.lol`) into .NET IL assemblies (`.dll`). It follows a **Roslyn-inspired architecture** with cleanly separated compilation phases, immutable data structures, and rich diagnostics.

**Key design principles:**
- Each phase is independently testable
- Immutable syntax and bound trees (no mutation after construction)
- Rich error reporting with source locations
- No external parser generators — everything is hand-rolled for maximum control

## Compiler Pipeline

```
Source Text (.lol)
       │
       ▼
┌──────────────┐
│    Lexer     │  Scans characters → SyntaxToken stream
│              │  Handles: keywords, identifiers, literals, whitespace, comments
└──────┬───────┘
       │  ImmutableArray<SyntaxToken>
       ▼
┌──────────────┐
│   Parser     │  Consumes tokens → Syntax Tree (AST)
│              │  Recursive descent with error recovery
└──────┬───────┘
       │  CompilationUnitSyntax (root node)
       ▼
┌──────────────┐
│   Binder     │  Walks AST → Bound Tree
│              │  Resolves: variables, functions, types
│              │  Reports: semantic errors, type mismatches
└──────┬───────┘
       │  BoundCompilationUnit (root bound node)
       ▼
┌──────────────┐
│  Lowering    │  (Optional) Simplifies bound tree
│              │  Transforms complex constructs into primitives
└──────┬───────┘
       │  Simplified BoundTree
       ▼
┌──────────────┐
│   Emitter    │  Walks bound tree → CIL opcodes
│              │  Uses PersistedAssemblyBuilder
│              │  Outputs: .dll + .runtimeconfig.json
└──────┬───────┘
       │
       ▼
  Output.dll (runnable with `dotnet Output.dll`)
```

## Component Details

### 1. Lexer (Tokenizer)

**Purpose:** Convert raw source text into a stream of classified tokens.

**Design:**
- Hand-rolled character-by-character scanner
- Tracks position (line, column, absolute offset) for diagnostics
- Produces `SyntaxToken` records: `(SyntaxKind Kind, string Text, object? Value, TextSpan Span)`
- Multi-word keywords (e.g., `I HAS A`, `IM IN YR`) are recognized as single tokens via lookahead
- Handles LOLCODE-specific escapes in strings (`:)` = newline, `:>` = tab, `::` = colon, `:"` = quote)
- Handles string interpolation `:{var}` within YARN literals
- `AN` keyword is optional between operands per spec

**Key challenges:**
- LOLCODE has many multi-word keywords requiring lookahead
- Line-based grammar (statements are newline-delimited, not semicolons)
- Comment syntax: `BTW` (to end of line), `OBTW`...`TLDR` (block)

**Output:** `ImmutableArray<SyntaxToken>`

### 2. Parser (AST Construction)

**Purpose:** Consume tokens and build an Abstract Syntax Tree representing the program's syntactic structure.

**Design:**
- Recursive descent parser (like Roslyn's)
- Each grammar rule maps to a parsing method (e.g., `ParseIfStatement()`, `ParseExpression()`)
- Error recovery: on unexpected tokens, skip to next newline (statement boundary sync)
- Produces immutable AST nodes (C# records)
- Handles LOLCODE's prefix notation for operators (`SUM OF x AN y` instead of `x + y`)

**AST Node Hierarchy:**
```
SyntaxNode (abstract)
├── CompilationUnitSyntax          # Root: HAI ... KTHXBYE
├── StatementSyntax (abstract)
│   ├── VariableDeclarationSyntax  # I HAS A x ITZ 42
│   ├── AssignmentSyntax           # x R 100
│   ├── PrintSyntax                # VISIBLE expr
│   ├── InputSyntax                # GIMMEH var
│   ├── IfSyntax                   # O RLY? / YA RLY / NO WAI / OIC
│   ├── SwitchSyntax               # WTF? / OMG / OMGWTF / OIC
│   ├── LoopSyntax                 # IM IN YR ... IM OUTTA YR
│   ├── FunctionDeclarationSyntax  # HOW IZ I ... IF U SAY SO
│   ├── ReturnSyntax               # FOUND YR expr
│   ├── BreakSyntax                # GTFO
│   ├── CastSyntax                 # var IS NOW A TYPE
│   └── ExpressionStatementSyntax  # bare expression (result → IT)
└── ExpressionSyntax (abstract)
    ├── LiteralExpressionSyntax    # 42, 3.14, "yarn", WIN, FAIL
    ├── VariableExpressionSyntax   # x, IT
    ├── BinaryExpressionSyntax     # SUM OF x AN y
    ├── UnaryExpressionSyntax      # NOT x
    ├── ComparisonExpressionSyntax # BOTH SAEM x AN y
    ├── CastExpressionSyntax       # MAEK x A NUMBR
    ├── SmooshExpressionSyntax     # SMOOSH x AN y MKAY
    ├── InterpolatedStringExpressionSyntax # "hai :{var}!"
    ├── FunctionCallExpressionSyntax # I IZ func YR arg MKAY
    └── NaryExpressionSyntax       # ALL OF x AN y AN z MKAY
```

### 3. Binder (Semantic Analysis)

**Purpose:** Walk the AST, resolve symbols, check types, and produce a semantically validated bound tree.

**Responsibilities:**
- Variable declaration and scope tracking
- Function declaration and parameter validation
- Type inference and coercion (LOLCODE is loosely typed)
- Implicit `IT` variable management
- Error reporting for: undeclared variables, type mismatches, invalid operations, etc.

**Type coercion rules (LOLCODE-specific):**
- `NOOB` → `TROOF` = `FAIL` (this is the **only** implicit NOOB cast)
- `NOOB` in any other context without explicit cast = **runtime error**
- `NUMBR` + `NUMBAR` → `NUMBAR` (float wins)
- `YARN` in arithmetic → attempt parse to `NUMBR` (no decimal point) or `NUMBAR` (has decimal point); non-numeric YARN = error
- Any type → `TROOF`: empty/zero/null = `FAIL`, everything else = `WIN`
- **Equality comparisons have NO automatic casting**: `BOTH SAEM "3" AN 3` is `FAIL`
- Functions with no explicit `FOUND YR` return the value of `IT` at the end of the code block

**Output:** `BoundTree` with `BoundNode` hierarchy (mirrors AST but with resolved type information)

### 4. Lowering (Optional)

**Purpose:** Transform complex bound nodes into simpler primitives for easier IL emission.

**Examples:**
- `IM IN YR` loop with `UPPIN`/`NERFIN` → while-loop with explicit increment/decrement
- `SMOOSH` with multiple arguments → chain of string concatenations
- `ALL OF` / `ANY OF` → chain of `AND` / `OR` operations
- `BIGGR OF` / `SMALLR OF` → conditional branch

### 5. Emitter (IL Code Generation)

**Purpose:** Walk the bound tree and emit CIL opcodes that implement the program's semantics.

**Technology:** `System.Reflection.Emit.PersistedAssemblyBuilder` (.NET 9+)

**IL mapping for key constructs:**

| LOLCODE Construct | CIL Implementation |
|---|---|
| `VISIBLE expr` | `ldstr` / `ldloc` + `call Console.WriteLine` |
| `GIMMEH var` | `call Console.ReadLine` + `stloc` |
| `I HAS A var ITZ val` | `.locals init` + `stloc` |
| `SUM OF x AN y` | `ldloc x` + `ldloc y` + `add` |
| `BOTH SAEM x AN y` | `ldloc x` + `ldloc y` + `ceq` |
| `O RLY? / YA RLY / NO WAI` | `brfalse` / `br` labels |
| `IM IN YR` loop | Labels + `br` back to top |
| `GTFO` | `br` to loop exit label |
| `HOW IZ I func` | `DefineMethod` + emit body |
| `I IZ func YR arg MKAY` | `ldarg` + `call` |
| `FOUND YR expr` | `ldloc/ldarg` + `ret` |

**Output files:**
- `<name>.dll` — The compiled .NET assembly
- `<name>.runtimeconfig.json` — Runtime configuration for `dotnet` host

### 6. Driver (CLI)

**Commands:**
- `lolcode compile <file.lol> [-o output.dll]` — Compile to DLL
- `lolcode run <file.lol>` — Compile and execute
- `lolcode repl` — Interactive LOLCODE session (stretch goal)

**Features:**
- Colored diagnostic output with source context
- Exit codes: 0 = success, 1 = compilation errors, 2 = runtime error

### 7. Diagnostics

**Design:** Roslyn-style diagnostic system.

- Each diagnostic has: ID (e.g., `LOL0001`), severity (Error/Warning/Info), message, source span
- `DiagnosticBag` collects diagnostics from all phases
- Pretty-printed with source line context and squiggly underlines

**Example output:**
```
error LOL0001: Undeclared variable 'x'
  --> hello.lol:5:3
   |
 5 |   VISIBLE x
   |           ^
```

---

## Type System Mapping

| LOLCODE Type | .NET Type | IL Type | Default Value |
|-------------|-----------|---------|---------------|
| `NUMBR` | `System.Int32` | `int32` | `0` |
| `NUMBAR` | `System.Double` | `float64` | `0.0` |
| `YARN` | `System.String` | `string` | `""` |
| `TROOF` | `System.Boolean` | `bool` | `FAIL` (false) |
| `NOOB` | `System.Object` (null) | `object` | `null` |
| `TYPE` | `System.String` (bare word) | `string` | N/A |

> **Note:** `BUKKIT` is reserved in the 1.2 spec with no defined syntax. This compiler does not implement it and produces an error if used.

---

## Runtime Type Representation

LOLCODE is dynamically typed — variables can change type at any time. This poses a challenge for .NET IL, which is statically typed at the opcode level.

### Strategy: Object-Backed Variables with Runtime Helpers

All LOLCODE variables are emitted as `System.Object` locals in IL. Arithmetic, comparison, and boolean operations use **runtime helper methods** that:
1. Inspect the runtime type of operands
2. Perform implicit coercion per LOLCODE rules
3. Execute the operation
4. Return the result (boxed if needed)

### Runtime Helper Library (`Lolcode.Runtime`)

A small runtime support assembly provides these helpers, referenced by all compiled LOLCODE programs:

| Helper Method | Purpose |
|--------------|---------|
| `LolRuntime.IsTruthy(object)` → `bool` | Evaluate truthiness per LOLCODE rules |
| `LolRuntime.Coerce(object, LolType)` → `object` | Explicit cast with LOLCODE semantics |
| `LolRuntime.Add(object, object)` → `object` | `SUM OF` with type promotion |
| `LolRuntime.Subtract(object, object)` → `object` | `DIFF OF` with type promotion |
| `LolRuntime.Multiply(object, object)` → `object` | `PRODUKT OF` with type promotion |
| `LolRuntime.Divide(object, object)` → `object` | `QUOSHUNT OF` with type promotion |
| `LolRuntime.Modulo(object, object)` → `object` | `MOD OF` with type promotion |
| `LolRuntime.Equal(object, object)` → `bool` | `BOTH SAEM` with type awareness |
| `LolRuntime.Concat(object[])` → `string` | `SMOOSH` — cast all to YARN and join |
| `LolRuntime.Print(object, bool)` → `void` | `VISIBLE` with optional newline suppression |
| `LolRuntime.ReadInput()` → `string` | `GIMMEH` wrapper |

### IL Emission Example (Dynamic)

For `SUM OF x AN y` where `x` and `y` are dynamically typed:
```
ldloc x           // push object
ldloc y           // push object
call LolRuntime.Add(object, object)  // returns object
stloc result      // store result
```

### Optimization: Static Type Specialization

When the binder can **prove** the types at compile time (e.g., `I HAS A x ITZ 42` is always `NUMBR`), the emitter can use native IL opcodes directly:
```
ldloc.0           // load int32
ldloc.1           // load int32
add               // native int add
stloc.2           // store int32
```

This optimization avoids boxing/unboxing overhead for the common case where types are known.

---

## IL Emission Strategy

We use `PersistedAssemblyBuilder` (new in .NET 9, available in .NET 10):

```csharp
var ab = new PersistedAssemblyBuilder(
    new AssemblyName("MyProgram"),
    typeof(object).Assembly
);
var mb = ab.DefineDynamicModule("MainModule");
var tb = mb.DefineType("Program", TypeAttributes.Public | TypeAttributes.Class);
var main = tb.DefineMethod("Main",
    MethodAttributes.Public | MethodAttributes.Static,
    typeof(void), [typeof(string[])]);

ILGenerator il = main.GetILGenerator();
// ... emit IL opcodes ...
il.Emit(OpCodes.Ret);

tb.CreateType();
ab.Save("MyProgram.dll");
```

**Why not Roslyn/transpile to C#?**
- Direct IL gives us full control over the generated code
- No C# intermediate step means faster compilation
- Educational value — understanding IL is the point
- `PersistedAssemblyBuilder` in .NET 10 makes this straightforward

**Why not `System.Reflection.Metadata` / ECMA-335 directly?**
- `Reflection.Emit` provides a higher-level API (ILGenerator, DefineMethod, etc.)
- Lower-level metadata writing is possible as a fallback if needed

---

## MSBuild SDK Integration

The `Lolcode.Sdk` package enables:

```xml
<Project Sdk="Lolcode.Sdk/1.0.0">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>
```

**Implementation:**
- `Sdk.props` — Sets default properties, defines `*.lol` item group, imports framework references
- `Sdk.targets` — Defines `CompileLolcode` target that runs before `Build`, with proper `Inputs`/`Outputs` for incremental build support
- `LolcodeCompileTask` — Custom MSBuild task invoking the compiler library directly (in-process) or via CLI tool

**NuGet SDK Package Layout:**
```
Lolcode.Sdk.nupkg/
├── Sdk/
│   ├── Sdk.props          # Auto-imported before project file
│   └── Sdk.targets        # Auto-imported after project file
├── tools/
│   └── LolcodeCompileTask.dll  # The MSBuild task assembly
└── Lolcode.Sdk.nuspec
```

**Key considerations:**
- Incremental builds: `Inputs` are `*.lol` files, `Outputs` are the compiled DLL — MSBuild skips compilation if inputs haven't changed
- Design-time builds: The SDK must handle VS/VS Code design-time builds gracefully (no compilation, just provide item metadata)
- Dependency acquisition: `Lolcode.Runtime.dll` (the runtime helper library) must be included as a reference automatically
- The `Sdk.targets` generates the `.runtimeconfig.json` alongside the output DLL

This means `dotnet build` and `dotnet run` work natively with `.lol` projects.

---

## VS Code Extension Architecture

### Syntax Highlighting
- **TextMate grammar** (`lolcode.tmLanguage.json`) — regex-based token classification
- Scopes map to VS Code theme colors (keywords, strings, comments, types, etc.)

### Language Features (Planned)
- **Language Server Protocol (LSP)** via `OmniSharp.Extensions.LanguageServer`
  - Diagnostics (red squiggles)
  - Hover information
  - Go to definition
  - Autocomplete for keywords

### Debugging (Bonus)
- **Debug Adapter Protocol (DAP)** server
  - Breakpoints
  - Step through
  - Variable inspection
  - Call stack

---

## Key .NET APIs

| API | NuGet / Built-in | Purpose |
|-----|-------------------|---------|
| `System.Reflection.Emit.PersistedAssemblyBuilder` | Built-in (.NET 10) | Create and save dynamic assemblies to disk |
| `System.Reflection.Emit.ILGenerator` | Built-in | Emit CIL opcodes |
| `System.Reflection.Emit.OpCodes` | Built-in | All CIL opcode constants |
| `System.Reflection.AssemblyName` | Built-in | Assembly identity |
| `System.Collections.Immutable` | Built-in | Immutable collections for AST |
| `System.CommandLine` | NuGet | CLI argument parsing |
| `OmniSharp.Extensions.LanguageServer` | NuGet | LSP server (VS Code integration) |

---

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|-----------|
| LOLCODE grammar ambiguities | Parser complexity | Reference existing implementations, document deviations from spec |
| `PersistedAssemblyBuilder` limitations | Can't emit certain constructs | Fall back to `System.Reflection.Metadata` for edge cases |
| IL bugs are hard to debug | Time spent debugging emitter | Use `ildasm` / ILSpy to inspect output, compare with C# compiler output |
| Multi-word keyword parsing | Lexer complexity | Careful lookahead design, extensive keyword tests |
| Loose typing / coercion edge cases | Runtime errors | Document all coercion rules, comprehensive type tests |
| Scope creep | Project never finishes | Strict phase gates — Phase 4 (working compiler) is MVP |
