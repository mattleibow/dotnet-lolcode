# Roadmap

Build phases for the dotnet-lolcode compiler. Each phase builds on the previous one.

## Phase 0: Project Scaffolding
> Set up the solution structure, build configuration, and initial project files.

- [x] Initialize git repo with `.gitignore`
- [x] Create `dotnet-lolcode.sln`
- [x] Create `Directory.Build.props` (net10.0, C# 14, nullable, implicit usings)
- [x] Create `Lolcode.CodeAnalysis` class library project
- [x] Create `Lolcode.Cli` console project
- [x] Create `Lolcode.CodeAnalysis.Tests` xUnit test project
- [x] Verify `dotnet build` and `dotnet test` work
- [x] Prototype: emit a minimal "hello world" IL assembly using `PersistedAssemblyBuilder` to validate the API works on target platform

## Phase 1: Lexer (Tokenizer)
> Convert source text into a classified token stream.
>
> **Depends on:** Phase 0

- [x] Define `SyntaxKind` enum (all keywords, operators, literals, trivia)
- [x] Implement `SyntaxToken` record (Kind, Text, Value, Span)
- [x] Implement `SourceText` with line/column tracking
- [x] Implement `TextSpan` and `TextLocation` for diagnostics
- [x] Implement `DiagnosticBag` for error collection
- [x] Implement `Lexer` — hand-rolled character scanner
  - [x] Single-character tokens (commas, newlines)
  - [x] Number literals (NUMBR and NUMBAR), including leading hyphen (`-`) for negatives
  - [x] String literals with LOLCODE escape sequences (`:)`, `:>`, `:o`, `:"`, `::`)
  - [x] String interpolation `:{var}` — emit `InterpolatedStringStart`/`Text`/`Variable`/`End` tokens
  - [x] Unicode escapes `:(<hex>)` and `:[<name>]` (curated subset of Unicode names)
  - [x] Multi-word keyword recognition with lookahead (state machine for `I HAS A`, `IM IN YR`, `HOW IZ I`, `BOTH SAEM`, etc.)
  - [x] Identifier recognition (case-sensitive, letters/digits/underscores, must start with letter)
  - [x] Comment handling (BTW to end of line, OBTW...TLDR block comments)
  - [x] Line continuation (`...` and `…`) — including rule: may not be followed by an empty line
  - [x] Soft-command-break (`,`) as virtual newline
  - [x] `!` token after VISIBLE (newline suppression)
  - [x] Commas and line continuation ignored inside quoted strings
  - [x] BTW ignores trailing `...` and `,` (comment always terminates at real newline)
  - [x] Report error for unterminated string literals
- [x] Write lexer tests for all token types
- [x] Write lexer edge case tests (partial multi-word keywords, `IM` as identifier, nested escapes)
- [x] Write lexer error tests (unterminated strings, continuation before empty line, invalid escapes)
- [x] Verify: can tokenize all sample programs

## Phase 2: Parser (AST Construction)
> Build an Abstract Syntax Tree from the token stream using recursive descent.
>
> **Depends on:** Phase 1

- [x] Define AST node base classes (`SyntaxNode`, `StatementSyntax`, `ExpressionSyntax`)
- [x] Define all statement node types
  - [x] `CompilationUnitSyntax` (HAI version ... KTHXBYE)
  - [x] `VariableDeclarationSyntax` (I HAS A)
  - [x] `AssignmentSyntax` (R)
  - [x] `PrintSyntax` (VISIBLE) — infinite arity, multiple expressions, `!` newline suppression flag
  - [x] `InputSyntax` (GIMMEH)
  - [x] `IfSyntax` (O RLY? / YA RLY / MEBBE / NO WAI / OIC)
  - [x] `SwitchSyntax` (WTF? / OMG / OMGWTF / OIC) — OMG values must be literal tokens
  - [x] `LoopSyntax` (IM IN YR / IM OUTTA YR)
  - [x] `FunctionDeclarationSyntax` (HOW IZ I / IF U SAY SO)
  - [x] `ReturnSyntax` (FOUND YR)
  - [x] `BreakSyntax` (GTFO)
  - [x] `CastSyntax` (IS NOW A)
  - [x] `ExpressionStatementSyntax`
- [x] Define all expression node types
  - [x] `LiteralExpressionSyntax` (numbers, strings, WIN, FAIL)
  - [x] `VariableExpressionSyntax` (identifiers, IT)
  - [x] `BinaryExpressionSyntax` (SUM OF, DIFF OF, PRODUKT OF, QUOSHUNT OF, MOD OF, BIGGR OF, SMALLR OF)
  - [x] `UnaryExpressionSyntax` (NOT)
  - [x] `ComparisonExpressionSyntax` (BOTH SAEM, DIFFRINT)
  - [x] `BooleanExpressionSyntax` (BOTH OF, EITHER OF, WON OF)
  - [x] `NaryExpressionSyntax` (ALL OF, ANY OF) — terminated by MKAY or EOL
  - [x] `CastExpressionSyntax` (MAEK ... [A] type)
  - [x] `SmooshExpressionSyntax` (SMOOSH ... MKAY) — terminated by MKAY or EOL
  - [x] `InterpolatedStringExpressionSyntax` (string with `:{var}` segments)
  - [x] `FunctionCallExpressionSyntax` (I IZ ... MKAY) — MKAY may be omitted at EOL
- [x] Implement `Parser` — recursive descent
  - [x] Token consumption helpers (Match, Expect, Peek)
  - [x] Error recovery (sync on newlines and commas)
  - [x] Statement parsing
  - [x] Expression parsing (prefix notation)
  - [x] Handle optional `AN` separator between binary operator arguments
  - [x] Handle `MKAY` omission at end of line/statement for variadic operators
  - [x] Comma as statement separator
  - [x] HAI version number parsing and validation
- [x] Write parser tests for each language construct
- [x] Write parser error recovery tests
- [x] Verify: can parse all sample programs

## Phase 3: Binder & Lowering (Semantic Analysis)
> Validate semantics, resolve types, produce a bound tree, and lower complex constructs.
>
> **Depends on:** Phase 2

- [x] Define type symbols (`TypeSymbol` for NUMBR, NUMBAR, YARN, TROOF, NOOB, TYPE)
- [x] Define `VariableSymbol` and `FunctionSymbol` (part of `Symbol` hierarchy)
- [x] Implement `BoundScope` (nested scopes for variables/functions)
- [x] Define bound node hierarchy (mirrors syntax nodes with type info)
- [x] Define `BoundBinaryOperatorKind` and `BoundUnaryOperatorKind` operator enums
- [x] Implement `Binder`
  - [x] Variable declaration and lookup
  - [x] Function declaration and lookup
  - [x] Enforce function scope isolation (no outer variable access)
  - [x] Type inference from literals and expressions
  - [x] Implicit type coercion rules (NOOB → TROOF only; YARN in math → parse as NUMBR/NUMBAR)
  - [x] NOOB restriction enforcement (error on implicit cast except TROOF; warn when statically provable)
  - [x] `IT` variable management per scope (see DESIGN.md §IT Variable Semantics)
  - [x] Implicit `IT` return from functions (when no FOUND YR)
  - [x] No automatic casting in equality comparisons (BOTH SAEM, DIFFRINT)
  - [x] Boolean operators auto-cast operands to TROOF
  - [x] Math type promotion: NUMBR+NUMBR=NUMBR, any NUMBAR→NUMBAR
  - [x] Comparison type promotion: same as math for numeric types
  - [x] NUMBAR → YARN truncation to 2 decimal places (casting rule)
  - [x] `WTF?` validation: OMG values must be literals (no expressions, no interpolated strings)
  - [x] `WTF?` validation: each OMG literal must be unique within the switch
  - [x] `GTFO` context tracking: control-flow stack for loop/switch/function (see DESIGN.md §GTFO Context Sensitivity)
  - [x] Loop iteration variable scoping (temporary, local to the loop)
  - [x] Loop operation validation: UPPIN, NERFIN, or valid unary function name
  - [x] `TYPE` type handling (bare word values, TYPE → TROOF casting, TYPE → YARN casting)
  - [x] `BUKKIT` usage detection → produce error diagnostic
  - [x] `GIMMEH` always stores as YARN type
  - [x] `HAI` version number handling (accept 1.2, warn on others)
  - [x] Semantic error reporting
- [x] Implement `Lowerer` in `Lowering/Lowerer.cs` (currently identity pass; simplifies bound tree for code generator)
  - [x] `UPPIN`/`NERFIN` loops → while-loop with explicit increment/decrement
  - [x] `SMOOSH` n-ary → chain of `String.Concat` calls
  - [x] `ALL OF` / `ANY OF` → short-circuit chain of `BOTH OF` / `EITHER OF`
  - [x] `BIGGR OF` / `SMALLR OF` → conditional branch or `Math.Max`/`Math.Min`
  - [x] `VISIBLE` with multiple args → concatenate YARN-cast values + print
  - [x] Interpolated strings → SMOOSH-equivalent concatenation
- [x] Write binder tests (positive and negative cases)
- [x] Write binder error tests (undeclared vars, type mismatches, NOOB misuse, BUKKIT usage)
- [x] Create conformance test matrix (one test per LANGUAGE_SPEC section × positive × negative)
- [x] Verify: all sample programs pass semantic analysis

## Phase 4: Code Generator (IL Emission)
> Generate .NET IL from the bound tree and save as a runnable DLL.
>
> **Depends on:** Phase 3

- [x] Create `Lolcode.Runtime` support library (runtime helpers)
  - [x] `LolRuntime.IsTruthy(object)` → truthiness evaluation
  - [x] `LolRuntime.Coerce(object, LolType)` → explicit casting with LOLCODE semantics
  - [x] `LolRuntime.Add/Subtract/Multiply/Divide/Modulo` → type-aware arithmetic with NUMBR/NUMBAR promotion
  - [x] `LolRuntime.Greater/Smaller` → type-aware BIGGR OF / SMALLR OF
  - [x] `LolRuntime.Equal/NotEqual` → type-aware comparison (no auto-cast across types)
  - [x] `LolRuntime.Concat(object[])` → SMOOSH implementation (cast all to YARN and join)
  - [x] `LolRuntime.CastToYarn(object)` → to-string with NUMBAR 2-decimal truncation (`"F2"`)
  - [x] `LolRuntime.Print(object[], bool)` → VISIBLE with infinite arity and newline suppression
  - [x] `LolRuntime.ReadInput()` → GIMMEH wrapper (always returns YARN)
- [x] Implement `CodeGenerator` (`Lolcode.CodeAnalysis.CodeGen`) with `PersistedAssemblyBuilder`
  - [x] Assembly and module setup
  - [x] Entry point (`Main` method) generation
  - [x] Local variable allocation (as `object` for dynamic typing)
  - [x] Literal loading (ldstr, ldc.i4, ldc.r8, ldnull for NOOB)
  - [x] Variable load/store (ldloc, stloc)
  - [x] `IT` variable allocation and management per scope
  - [x] `VISIBLE` → `LolRuntime.Print` (handles multiple args + `!` suppression)
  - [x] `GIMMEH` → `LolRuntime.ReadInput` (stores as YARN)
  - [x] Arithmetic → runtime helpers (or native IL opcodes when types statically known)
  - [x] Comparison → runtime helpers (no auto-cast across types)
  - [x] Boolean logic → IsTruthy + and/or/xor/not
  - [x] Conditionals → brfalse/brtrue + labels
  - [x] `WTF?` switch → labels per case with fall-through (no GTFO = continue to next OMG block)
  - [x] Loops → labels + br (branch back); GTFO → br to loop exit label
  - [x] Functions → DefineMethod + call; GTFO in function → ldnull + ret
  - [x] String concatenation → `LolRuntime.Concat` or `String.Concat`
  - [x] String interpolation → lowered to concatenation (handled by lowerer)
  - [x] Type casting → conversion opcodes / runtime helpers
  - [x] TYPE values → string representations
- [x] Generate `.runtimeconfig.json` alongside DLL
- [x] Reference `Lolcode.Runtime.dll` in output
- [x] Implement `--emit-il` flag to dump human-readable IL for debugging
- [x] Write code generator tests (compile → run → assert stdout)
- [x] Write IL structure tests (verify labels, branches, call sites using `--emit-il` or ILSpy)
- [x] Write coercion/casting test suite (every row in LANGUAGE_SPEC Casting Rules Summary)
- [x] Write `IT` lifetime tests (IT after function call, IT in nested conditionals, IT in loops)
- [x] Write `WTF?` fall-through tests (with and without GTFO)
- [x] Verify: `dotnet <output>.dll` works for all sample programs

## Phase 5: SDK & Tooling (**MVP Complete**)
> Package the compiler as a usable MSBuild SDK with file-based app support.
>
> **Depends on:** Phase 4

- [x] Create all 15 sample programs (graduated complexity)
- [x] Run conformance test suite: all samples compile and run with correct stdout/exit codes (343+ tests passing)
- [x] Write diagnostic snapshot tests (stable LOLxxxx IDs, error messages for common mistakes)
- [x] Verify: all 15 sample programs compile and run correctly
- [x] Pretty-print diagnostics with source context and colors
- [x] Exit codes: 0 = success, 1 = compile error, 2 = runtime error

> **Note:** The original CLI tool (`Lolcode.Cli`) was replaced by the MSBuild SDK approach. All functionality is now available via `dotnet build`, `dotnet run`, and `dotnet run --file`.

---

> **MVP milestone reached after Phase 5.** Everything below is enhancement.

---

> **Roslyn Alignment (Post-MVP):** After MVP completion, the codebase was restructured to align with Roslyn's architecture. Key changes: namespace rename (`Lolcode.Compiler` → `Lolcode.CodeAnalysis`), `Symbol` hierarchy, `SyntaxTree`/`LolcodeCompilation`/`EmitResult` types, `BoundScope`, `BoundBinaryOperatorKind`/`BoundUnaryOperatorKind` operator enums, `SyntaxFacts`, `Lowerer` pass (in `Lowering/Lowerer.cs`), `Emitter` → `CodeGenerator` rename (in `Lolcode.CodeAnalysis.CodeGen`), `DiagnosticDescriptor` pattern with `ErrorCode` enum and `DiagnosticDescriptors` catalog (in `Errors/`), and public API surface review (implementation types made `internal`, `InternalsVisibleTo` for tests).

---

## Phase 6: VS Code Extension
> Syntax highlighting, snippets, and build integration for VS Code.
>
> **Depends on:** Phase 5 (CLI tool must exist for build tasks)

- [ ] Scaffold VS Code extension (`yo code`)
- [ ] Create TextMate grammar (`lolcode.tmLanguage.json`)
  - [ ] Keywords (HAI, KTHXBYE, VISIBLE, etc.)
  - [ ] Type names (NUMBR, NUMBAR, YARN, TROOF, NOOB, TYPE)
  - [ ] Comments (BTW, OBTW...TLDR)
  - [ ] Strings with escape sequences
  - [ ] Number literals
  - [ ] Boolean literals (WIN, FAIL)
  - [ ] Operators and separators
- [ ] Create `language-configuration.json`
  - [ ] Comment toggling
  - [ ] Bracket matching (O RLY?/OIC, IM IN YR/IM OUTTA YR, etc.)
  - [ ] Auto-closing pairs
- [ ] Create code snippets
  - [ ] `hai` → full program template
  - [ ] `var` → variable declaration
  - [ ] `if` → conditional template
  - [ ] `loop` → loop template
  - [ ] `func` → function template
- [ ] Add build task definition for `lolcode compile`
- [ ] Test highlighting on all sample programs
- [ ] Package as `.vsix`

## Phase 7: MSBuild SDK Integration ✅
> Enable `dotnet build` and `dotnet run` for `.lol` projects.
>
> **Depends on:** Phase 5 (compiler CLI must work)

- [x] Create `Lolcode.NET.Sdk` NuGet package (`src/Lolcode.NET.Sdk/`)
- [x] Implement `Sdk.props` (language defaults, `**/*.lol` glob, disable C# analyzers)
- [x] Implement `Sdk.targets` (CoreCompile override with Lolc task, incremental builds, `dotnet watch`)
- [x] Implement `Lolc` MSBuild task (`src/Lolcode.Build/Lolc.cs`) — in-process compilation
- [x] Auto-reference `Lolcode.Runtime.dll` in output
- [x] Handle design-time builds (`SkipCompilerExecution` property)
- [x] Support `<Project Sdk="Lolcode.NET.Sdk">` in `.lolproj` files
- [x] Create `dotnet new lolcode` template (`src/Lolcode.NET.Templates/`)
- [x] Create SDK sample project (`samples/sdk-hello-world/`)
- [x] Create `pack-local.sh` for local SDK development
- [x] Test: `dotnet build` compiles `.lol` → `.dll`
- [x] Test: `dotnet run` executes the program
- [x] Enable file-based app support (`dotnet run --file hello.lol` with `#:sdk` directive)
- [x] Lexer skips `#:` directives and `#!` shebang lines
- [x] Sdk.targets filters `@(Compile)` to `.lol` files only
- [x] Remove CLI project (replaced by SDK + file-based apps)

## Phase 8: Debugging Support (Bonus)
> Enable VS Code debugging for LOLCODE programs.
>
> **Depends on:** Phase 4 (code generator) + Phase 6 (VS Code extension)

- [ ] Emit PDB debug symbols with source mapping
- [ ] Implement Debug Adapter Protocol (DAP) server
- [ ] VS Code extension: register debug type and configuration
- [ ] Support breakpoints
- [ ] Support step-through (step in, step over, step out)
- [ ] Support variable inspection
- [ ] Add `launch.json` template to extension

## Phase 9: Polish & Documentation
> Final polish, CI/CD, and community readiness.
>
> **Depends on:** All prior phases

- [ ] Comprehensive README with getting started guide
- [ ] Ensure all docs are accurate and complete
- [ ] Add GitHub Actions CI/CD (build + test on push/PR)
- [ ] Create release workflow (NuGet + VSIX publishing)
- [ ] Record demo GIF for README
- [ ] All 15 samples verified working end-to-end
- [ ] Consider: REPL mode (`lolcode repl`)
- [ ] Consider: Language Server Protocol for rich VS Code features
