# Architecture & Technical Design

This document describes the internal architecture of the LOLCODE .NET compiler, the technical decisions behind it, and the key components and APIs used.

## Table of Contents

- [Overview](#overview)
- [Roslyn Alignment](#roslyn-alignment)
- [Compiler Pipeline](#compiler-pipeline)
- [Component Details](#component-details)
  - [Lexer](#1-lexer-tokenizer)
  - [Parser](#2-parser-ast-construction)
  - [Binder](#3-binder-semantic-analysis)
  - [Lowering](#4-lowering)
  - [Code Generator](#5-code-generator-il-emission)
  - [Driver](#6-driver-cli)
  - [Diagnostics](#7-diagnostics)
- [`IT` Variable Semantics](#it-variable-semantics)
- [`GTFO` Context Sensitivity](#gtfo-context-sensitivity)
- [Type System Mapping](#type-system-mapping)
- [Runtime Type Representation](#runtime-type-representation)
- [IL Emission Strategy](#il-emission-strategy)
- [MSBuild SDK Integration](#msbuild-sdk-integration)
- [VS Code Extension Architecture](#vs-code-extension-architecture)
- [Key .NET APIs](#key-net-apis)
- [String Interpolation Strategy](#string-interpolation-strategy)
- [`HAI` Version Handling](#hai-version-handling)
- [Spec Deviations and Clarifications](#spec-deviations-and-clarifications)
- [Risks and Mitigations](#risks-and-mitigations)

---

## Overview

The LOLCODE compiler is a **multi-phase, ahead-of-time (AOT) compiler** that transforms LOLCODE 1.2 source files (`.lol`) into .NET IL assemblies (`.dll`). It follows a **Roslyn-inspired architecture** with cleanly separated compilation phases, immutable data structures, and rich diagnostics.

**Key design principles:**
- Each phase is independently testable
- Immutable syntax and bound trees (no mutation after construction)
- Rich error reporting with source locations
- No external parser generators — everything is hand-rolled for maximum control

## Roslyn Alignment

The compiler follows a **Roslyn-inspired architecture**, mirroring key types and patterns from `Microsoft.CodeAnalysis`:

- **Namespace:** `Lolcode.CodeAnalysis` (mirrors `Microsoft.CodeAnalysis`)
- **Key types:** `SyntaxTree`, `LolcodeCompilation`, `EmitResult`, `SyntaxFacts`
- **Symbol model:** `Symbol` (abstract base), `VariableSymbol`, `FunctionSymbol`, `ParameterSymbol`, `TypeSymbol`
- **Bound tree:** Separate `BoundTree/` folder with `BoundKind` enum and bound node types
- **Pipeline:** Lexer → Parser → Binder → Lowerer → CodeGenerator

**Project structure (`src/`):**
```
├── Lolcode.CodeAnalysis/    → Core compiler library
│   ├── Binding/             → Binder, BoundScope
│   ├── BoundTree/           → BoundNode types, BoundKind, operator enums
│   ├── CodeGen/             → CodeGenerator
│   ├── Errors/              → ErrorCode, DiagnosticDescriptors
│   ├── Lowering/            → Lowerer
│   ├── Symbols/             → Symbol, TypeSymbol, VariableSymbol, FunctionSymbol
│   ├── Syntax/              → SyntaxTree, SyntaxFacts, Lexer, Parser, syntax nodes
│   ├── Text/                → SourceText, TextSpan, TextLocation
│   └── LolcodeCompilation.cs, EmitResult, Diagnostic, DiagnosticDescriptor, DiagnosticBag
├── Lolcode.Runtime/         → Runtime helper library (referenced by compiled programs)
├── Lolcode.Build/           → MSBuild task (Lolc) for SDK integration
├── Lolcode.Cli/             → CLI tool (lolcode compile/run)
├── Lolcode.NET.Sdk/         → MSBuild SDK package (Sdk.props, Sdk.targets)
└── Lolcode.NET.Templates/   → dotnet new template pack
```

## Compiler Pipeline

```
Source Text (.lol)
       │
       ▼
┌─────────────────────────────┐
│  SyntaxTree.ParseText()     │  Entry point
│  ┌────────────────────────┐ │
│  │  Lexer                 │ │  Scans characters → SyntaxToken stream
│  │                        │ │  Keyword lookup via SyntaxFacts.GetKeywordKind()
│  └──────────┬─────────────┘ │
│             │                │
│  ┌──────────▼─────────────┐ │
│  │  Parser                │ │  Consumes tokens → CompilationUnitSyntax
│  │                        │ │  Recursive descent with error recovery
│  └────────────────────────┘ │
│  Returns: SyntaxTree        │  (Root, Diagnostics, FilePath)
└──────────────┬──────────────┘
               │
               ▼
┌─────────────────────────────┐
│  LolcodeCompilation.Create()│  Orchestrator
└──────────────┬──────────────┘
               │
┌──────────────▼──────────────┐
│   Binder                    │  Walks AST → Bound Tree
│                             │  Resolves: variables, functions, types
│                             │  Uses: BoundScope (parent chain), Symbol hierarchy
└──────────────┬──────────────┘
               │  BoundBlockStatement (root bound node)
               ▼
┌─────────────────────────────┐
│   Lowerer                   │  Rewrites bound tree (currently identity pass)
└──────────────┬──────────────┘
               │  Simplified BoundTree
               ▼
┌─────────────────────────────┐
│   CodeGenerator             │  Walks bound tree → CIL opcodes
│                             │  Uses PersistedAssemblyBuilder
│                             │  Outputs: .dll + .runtimeconfig.json
└──────────────┬──────────────┘
               │
               ▼
  EmitResult (Success, Diagnostics)
```

## Component Details

### 1. Lexer (Tokenizer)

**Purpose:** Convert raw source text into a stream of classified tokens.

**Design:**
- Hand-rolled character-by-character scanner
- Tracks position (line, column, absolute offset) for diagnostics
- Produces `SyntaxToken` records: `(SyntaxKind Kind, string Text, object? Value, TextSpan Span)`
- Multi-word keywords (e.g., `I HAS A`, `IM IN YR`) are recognized as single tokens via lookahead
- Keyword lookup is delegated to `SyntaxFacts.GetKeywordKind()`, which maps keyword text to `SyntaxKind` values
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
- Produces immutable AST nodes (C# sealed classes)
- Handles LOLCODE's prefix notation for operators (`SUM OF x AN y` instead of `x + y`)
- `SyntaxTree` wraps the parse result: `SyntaxTree.ParseText(source)` returns a `SyntaxTree` with `Root` (`CompilationUnitSyntax`), `Diagnostics` (`ImmutableArray<Diagnostic>`), and `FilePath` (`string?`)

**AST Node Hierarchy:**
```
SyntaxNode (abstract)
├── CompilationUnitSyntax          # Root: HAI ... KTHXBYE
├── ProgramStatementSyntax         # Program body (statements between HAI/KTHXBYE)
├── MebbeClauseSyntax              # MEBBE condition block
├── OmgClauseSyntax                # OMG case label
├── StatementSyntax (abstract)
│   ├── VariableDeclarationSyntax  # I HAS A x ITZ 42
│   ├── AssignmentStatementSyntax  # x R 100
│   ├── VisibleStatementSyntax     # VISIBLE expr
│   ├── GimmehStatementSyntax      # GIMMEH var
│   ├── IfStatementSyntax          # O RLY? / YA RLY / NO WAI / OIC
│   ├── SwitchStatementSyntax      # WTF? / OMG / OMGWTF / OIC
│   ├── LoopStatementSyntax        # IM IN YR ... IM OUTTA YR
│   ├── FunctionDeclarationSyntax  # HOW IZ I ... IF U SAY SO
│   ├── ReturnStatementSyntax      # FOUND YR expr
│   ├── GtfoStatementSyntax        # GTFO
│   ├── CastStatementSyntax        # var IS NOW A TYPE
│   ├── BlockStatementSyntax       # Grouped statements
│   └── ExpressionStatementSyntax  # bare expression (result → IT)
└── ExpressionSyntax (abstract)
    ├── LiteralExpressionSyntax    # 42, 3.14, "yarn", WIN, FAIL
    ├── VariableExpressionSyntax   # x
    ├── ItExpressionSyntax         # IT (implicit variable)
    ├── BinaryExpressionSyntax     # SUM OF x AN y, BOTH OF, EITHER OF, WON OF
    ├── UnaryExpressionSyntax      # NOT x
    ├── ComparisonExpressionSyntax # BOTH SAEM x AN y
    ├── DiffrintExpressionSyntax   # DIFFRINT x AN y
    ├── AllOfExpressionSyntax      # ALL OF x AN y AN z MKAY
    ├── AnyOfExpressionSyntax      # ANY OF x AN y AN z MKAY
    ├── CastExpressionSyntax       # MAEK x A NUMBR
    ├── SmooshExpressionSyntax     # SMOOSH x AN y MKAY
    └── FunctionCallExpressionSyntax # I IZ func YR arg MKAY
```

### 3. Binder (Semantic Analysis)

**Purpose:** Walk the AST, resolve symbols, check types, and produce a semantically validated bound tree.

**Responsibilities:**
- Variable declaration and scope tracking via `BoundScope` (parent chain for nested scopes)
- Function declaration and parameter validation
- Type inference and coercion (LOLCODE is loosely typed)
- Implicit `IT` variable management
- Error reporting for: undeclared variables, type mismatches, invalid operations, etc.

**Symbol hierarchy (`Lolcode.CodeAnalysis.Symbols`):**
- `Symbol` — abstract base
- `VariableSymbol` — declared variables and the implicit `IT`
- `FunctionSymbol` — function declarations (`HOW IZ I`)
- `ParameterSymbol` — function parameters (`YR`)
- `TypeSymbol` — built-in types (`NUMBR`, `NUMBAR`, `YARN`, `TROOF`, `NOOB`)

**Operator enums (`Lolcode.CodeAnalysis.BoundTree`):**
- `BoundBinaryOperatorKind` — Addition, Subtraction, Multiplication, Division, Modulo, LogicalAnd, LogicalOr, LogicalXor, Equal, NotEqual, etc.
- `BoundUnaryOperatorKind` — LogicalNot

**Type coercion rules (LOLCODE-specific):**
- `NOOB` → `TROOF` = `FAIL` (this is the **only** implicit NOOB cast)
- `NOOB` in any other context without explicit cast = **runtime error**
- `NUMBR` + `NUMBAR` → `NUMBAR` (float wins)
- `YARN` in arithmetic → attempt parse to `NUMBR` (no decimal point) or `NUMBAR` (has decimal point); non-numeric YARN = error
- Any type → `TROOF`: empty/zero/null = `FAIL`, everything else = `WIN`
- **Equality comparisons have NO automatic casting**: `BOTH SAEM "3" AN 3` is `FAIL`
- Functions with no explicit `FOUND YR` return the value of `IT` at the end of the code block

**Output:** Bound tree with `BoundNode` hierarchy (mirrors AST but with resolved type information), rooted at `BoundBlockStatement`

### 4. Lowering

**Purpose:** Transform complex bound nodes into simpler primitives for easier IL emission. Implemented as a separate `Lowering/Lowerer.cs` class with tree rewriting infrastructure.

**Current state:** The `Lowerer` is an identity pass — it traverses the bound tree and returns it unchanged. The rewriting infrastructure is in place for future desugaring transforms.

**Planned transforms:**
- `IM IN YR` loop with `UPPIN`/`NERFIN` → while-loop with explicit increment/decrement
- `SMOOSH` with multiple arguments → chain of `String.Concat` calls
- `ALL OF` / `ANY OF` → short-circuit chain of `BOTH OF` / `EITHER OF` operations
- `BIGGR OF` / `SMALLR OF` → conditional branch
- `VISIBLE` with multiple arguments → concatenation of YARN-cast values + print
- Interpolated strings → `SMOOSH`-equivalent concatenation of segments

### 5. Code Generator (IL Emission)

**Purpose:** Walk the bound tree and emit CIL opcodes that implement the program's semantics.

**Class:** `CodeGenerator` in namespace `Lolcode.CodeAnalysis.CodeGen`

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

**API flow:**
```csharp
var tree = SyntaxTree.ParseText(source, filePath);
var compilation = LolcodeCompilation.Create(tree);
var result = compilation.Emit(outputPath, runtimePath);
// result is EmitResult with Success (bool) and Diagnostics
```

**Features:**
- Colored diagnostic output with source context
- Exit codes: 0 = success, 1 = compilation errors, 2 = runtime error

### 7. Diagnostics

**Design:** Roslyn-style diagnostic system with descriptors.

- Each diagnostic category is defined by a `DiagnosticDescriptor` (ID, title, message format, category, severity)
- All descriptors are cataloged in `Errors/DiagnosticDescriptors.cs` with an `ErrorCode` enum
- `DiagnosticBag` (internal) collects diagnostics; consumers receive `ImmutableArray<Diagnostic>`
- `Diagnostic.Create(descriptor, location, args...)` stamps out instances from descriptors
- Pretty-printed with source line context and squiggly underlines

**Diagnostic ID ranges:**

| Range | Category | Example |
|-------|----------|---------|
| LOL0xxx | Lexer | LOL0001 Unexpected character |
| LOL1xxx | Parser | LOL1001 Unexpected token |
| LOL2xxx | Binder | LOL2001 Undeclared variable |
| LOL9xxx | Internal | LOL9001 Internal compiler error |

**Example output:**
```
error LOL0001: Undeclared variable 'x'
  --> hello.lol:5:3
   |
 5 |   VISIBLE x
   |           ^
```

**Public API surface:**
- `Diagnostic` — record with Id, Location, Message, Severity, Descriptor
- `DiagnosticDescriptor` — defines a category of diagnostic
- `DiagnosticDescriptors` — static catalog of all compiler diagnostics
- `DiagnosticSeverity` — Error, Warning, Info
- `ErrorCode` — enum of all LOLxxxx codes

Implementation details (`Binder`, `BoundScope`, `CodeGenerator`, `Lowerer`, `Lexer`, `Parser`, `DiagnosticBag`, all bound tree types) are `internal`. Tests access them via `[InternalsVisibleTo]`.

---

## `IT` Variable Semantics

The implicit `IT` variable is central to LOLCODE's control flow. These rules govern its behavior:

**Scope:**
- Each scope (main program block, each function body) has its own independent `IT` variable.
- `IT` is **not** passed into functions and is not accessible from outer scopes.
- Loop bodies share the same `IT` as their enclosing scope (loops do not create a new `IT`).
- `IT` starts as `NOOB` at the beginning of each scope.

**Updates:**
- Any bare expression statement (not an assignment, not a declaration) stores its result in `IT`.
- `IT` retains its value until the next bare expression replaces it.
- Assignment statements (`x R expr`) do **not** update `IT`.
- Variable declarations with initialization (`I HAS A x ITZ expr`) do **not** update `IT`.

**Usage in control flow:**
- `O RLY?` reads `IT` and branches based on its truthiness (cast to `TROOF`).
- `WTF?` reads `IT` as the scrutinee for `OMG` comparisons.
- These constructs expect `IT` to have been set by a preceding expression; using them when `IT` is `NOOB` is valid (NOOB is `FAIL` for `O RLY?`, no match for `WTF?`).

**Function returns:**
- If a function reaches `IF U SAY SO` without an explicit `FOUND YR` or `GTFO`, the current value of `IT` in that function's scope is returned.

**IL representation:**
- `IT` is emitted as a regular `System.Object` local variable in each method/scope.

---

## `GTFO` Context Sensitivity

`GTFO` has different semantics depending on the enclosing context:

| Context | Behavior |
|---------|----------|
| Inside a loop (`IM IN YR ... IM OUTTA YR`) | Breaks out of the innermost loop |
| Inside a switch (`WTF? ... OIC`) | Breaks out of the current `OMG` case (prevents fall-through) |
| Inside a function (`HOW IZ I ... IF U SAY SO`) | Returns `NOOB` from the function |
| Nested loop inside switch (or vice versa) | Breaks the **innermost** enclosing loop or switch |

The binder must maintain a **control-flow context stack** to determine the correct behavior for each `GTFO` statement and to report errors if `GTFO` appears outside any valid context.

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
| `LolRuntime.Greater(object, object)` → `object` | `BIGGR OF` (max) |
| `LolRuntime.Smaller(object, object)` → `object` | `SMALLR OF` (min) |
| `LolRuntime.CastToYarn(object)` → `string` | To-string with NUMBAR 2-decimal truncation |

### IL Emission Example (Dynamic)

For `SUM OF x AN y` where `x` and `y` are dynamically typed:
```
ldloc x           // push object
ldloc y           // push object
call LolRuntime.Add(object, object)  // returns object
stloc result      // store result
```

### Optimization: Static Type Specialization

When the binder can **prove** the types at compile time (e.g., `I HAS A x ITZ 42` is always `NUMBR`), the code generator can use native IL opcodes directly:
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

The `Lolcode.NET.Sdk` package enables building LOLCODE projects with standard .NET tooling:

```xml
<Project Sdk="Lolcode.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

Save as `MyApp.lolproj`, then `dotnet build` and `dotnet run` work natively.

**Architecture:**

| Component | Location | Purpose |
|-----------|----------|---------|
| `Sdk.props` | `src/Lolcode.NET.Sdk/Sdk/` | Imports Microsoft.NET.Sdk, sets `Language=LOLCODE`, globs `**/*.lol`, disables C# analyzers |
| `Sdk.targets` | `src/Lolcode.NET.Sdk/Sdk/` | Overrides `CoreCompile` target with `Lolc` task, auto-references `Lolcode.Runtime.dll`, supports `dotnet watch` |
| `Lolc` task | `src/Lolcode.Build/Lolc.cs` | MSBuild task (`Lolcode.Build.Lolc`) — invokes `LolcodeCompilation` in-process, maps diagnostics to MSBuild errors/warnings |

**Key design decisions:**
- **Layered SDK:** Extends `Microsoft.NET.Sdk` (imported in both props and targets) to inherit TFM resolution, NuGet restore, publish, clean, and run support
- **`CoreCompile` override:** Replaces the default C# `Csc` task with `Lolc`, using `Inputs`/`Outputs` for incremental builds
- **Design-time builds:** `SkipCompilerExecution` property allows VS/VS Code to gather metadata without compiling
- **Runtime reference:** `Lolcode.Runtime.dll` is automatically added as a `<Reference>` so it appears in `deps.json` and gets copied to output
- **`CreateManifestResourceNames`:** No-op target (LOLCODE has no embedded resources)

**NuGet SDK Package Layout:**
```
Lolcode.NET.Sdk.nupkg/
├── Sdk/
│   ├── Sdk.props          # Auto-imported before .lolproj
│   └── Sdk.targets        # Auto-imported after .lolproj
├── tools/
│   └── net10.0/
│       ├── Lolcode.Build.dll           # MSBuild Lolc task
│       ├── Lolcode.Build.deps.json     # Task dependencies
│       ├── Lolcode.CodeAnalysis.dll    # Compiler library
│       └── Lolcode.Runtime.dll         # Runtime helpers
└── Lolcode.NET.Sdk.nuspec
```

**`dotnet new` Template:** The `Lolcode.NET.Templates` package provides a `dotnet new lolcode` template that scaffolds a minimal `.lolproj` + `Program.lol`.

This means `dotnet build`, `dotnet run`, `dotnet publish`, `dotnet clean`, and `dotnet watch` all work natively with `.lol` projects.

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

## String Interpolation Strategy

String interpolation (`:{var}`) spans multiple compiler phases:

1. **Lexer:** Scans an interpolated string and emits a sequence of tokens — `InterpolatedStringStart`, `InterpolatedStringText` (literal segments), `InterpolatedStringVariable` (variable references), and `InterpolatedStringEnd`. The lexer does **not** resolve variables.
2. **Parser:** Consumes the interpolated string tokens and builds an `InterpolatedStringExpressionSyntax` node containing `LiteralExpressionSyntax` segments and `VariableExpressionSyntax` references.
3. **Binder:** Resolves variable references within the interpolated string. Reports errors for undeclared variables.
4. **Lowering:** Transforms `InterpolatedStringExpression` into the equivalent of `SMOOSH segment1 AN segment2 ... MKAY` (a chain of YARN casts and concatenation).
5. **Code Generator:** Emits the lowered concatenation as `LolRuntime.Concat` or `String.Concat` calls.

> **Note:** `OMG` case labels in `WTF?` blocks must be literals. Interpolated strings containing `:{var}` are **not** literals and must be rejected by the binder.

---

## `HAI` Version Handling

The parser validates the `HAI` version number:
- `HAI 1.2` — accepted (target version)
- `HAI` without a version — accepted with a warning (assumes 1.2)
- `HAI <other>` — accepted with an informational diagnostic noting the version is not 1.2
- Missing `HAI` — error
- Missing `KTHXBYE` — error

---

## Spec Deviations and Clarifications

This section records intentional implementation decisions where the LOLCODE 1.2 spec is ambiguous or under-specified:

| Area | Spec Says | This Compiler Does |
|------|-----------|-------------------|
| **`BUKKIT` type** | "Reserved for future expansion" | Produces error `LOL0xxx: BUKKIT is not supported in LOLCODE 1.2` |
| **`TYPE` equality** | "Under current review" | Compares TYPE values as strings (e.g., `BOTH SAEM mytype AN NUMBR` → string equality) |
| **Integer overflow** | Not specified | Wraps (standard .NET `int32` overflow behavior, unchecked) |
| **Float precision** | Not specified | Uses `System.Double` (IEEE 754 double-precision) |
| **`NUMBAR` → `YARN`** | "Truncates to two decimal places" | Uses `value.ToString("F2")` |
| **`:o` escape** | "Bell (beep)" | Maps to `\a` (U+0007, ASCII BEL) |
| **`:[<name>]`** | "Unicode normative name" | Supported with a curated subset of common Unicode names; unsupported names produce an error |
| **`HAI` version** | "No current standard behavior" | Accepts any version with informational diagnostic; targets 1.2 semantics |
| **Unary function in loop** | `<operation>` can be "any unary function" | Supports `UPPIN`, `NERFIN`, and user-defined unary functions by name |
| **`NOOB` in non-TROOF context** | "Results in an error" | Runtime error via `LolRuntime` helpers (binder warns when statically provable) |

---

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|-----------|
| LOLCODE grammar ambiguities | Parser complexity | Reference existing implementations, document deviations from spec |
| `PersistedAssemblyBuilder` limitations | Can't emit certain constructs | Fall back to `System.Reflection.Metadata` for edge cases; prototype IL emission early (Phase 0) |
| IL bugs are hard to debug | Time spent debugging code generator | Use `ildasm` / ILSpy to inspect output, compare with C# compiler output; add `--emit-il` flag in Phase 4 |
| Multi-word keyword parsing | Lexer complexity | Careful lookahead design, extensive keyword tests, state machine for multi-word tokens |
| Loose typing / coercion edge cases | Runtime errors | Document all coercion rules, comprehensive type tests, conformance test matrix |
| Scope creep | Project never finishes | Strict phase gates — Phase 5 (working compiler) is MVP; Phases 6–9 are post-MVP enhancements |
| `IT` variable semantics | Subtle semantic bugs | Rigorous definition (see §IT Variable Semantics above), dedicated test suite |
| `GTFO` context sensitivity | Wrong break/return behavior | Control-flow context stack in binder (see §GTFO Context Sensitivity above) |
| Dynamic typing performance | Boxing/unboxing overhead | Object-backed variables for MVP; static type specialization as optional Phase 4 optimization |
