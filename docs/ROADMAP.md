# Roadmap

Build phases for the dotnet-lolcode compiler. Each phase builds on the previous one.

## Phase 0: Project Scaffolding
> Set up the solution structure, build configuration, and initial project files.

- [ ] Initialize git repo with `.gitignore`
- [ ] Create `dotnet-lolcode.sln`
- [ ] Create `Directory.Build.props` (net10.0, C# 14, nullable, implicit usings)
- [ ] Create `Lolcode.CodeAnalysis` class library project
- [ ] Create `Lolcode.Cli` console project
- [ ] Create `Lolcode.CodeAnalysis.Tests` xUnit test project
- [ ] Verify `dotnet build` and `dotnet test` work
- [ ] Prototype: emit a minimal "hello world" IL assembly using `PersistedAssemblyBuilder` to validate the API works on target platform

## Phase 1: Lexer (Tokenizer)
> Convert source text into a classified token stream.
>
> **Depends on:** Phase 0

- [ ] Define `SyntaxKind` enum (all keywords, operators, literals, trivia)
- [ ] Implement `SyntaxToken` record (Kind, Text, Value, Span)
- [ ] Implement `SourceText` with line/column tracking
- [ ] Implement `TextSpan` and `TextLocation` for diagnostics
- [ ] Implement `DiagnosticBag` for error collection
- [ ] Implement `Lexer` — hand-rolled character scanner
  - [ ] Single-character tokens (commas, newlines)
  - [ ] Number literals (NUMBR and NUMBAR), including leading hyphen (`-`) for negatives
  - [ ] String literals with LOLCODE escape sequences (`:)`, `:>`, `:o`, `:"`, `::`)
  - [ ] String interpolation `:{var}` — emit `InterpolatedStringStart`/`Text`/`Variable`/`End` tokens
  - [ ] Unicode escapes `:(<hex>)` and `:[<name>]` (curated subset of Unicode names)
  - [ ] Multi-word keyword recognition with lookahead (state machine for `I HAS A`, `IM IN YR`, `HOW IZ I`, `BOTH SAEM`, etc.)
  - [ ] Identifier recognition (case-sensitive, letters/digits/underscores, must start with letter)
  - [ ] Comment handling (BTW to end of line, OBTW...TLDR block comments)
  - [ ] Line continuation (`...` and `…`) — including rule: may not be followed by an empty line
  - [ ] Soft-command-break (`,`) as virtual newline
  - [ ] `!` token after VISIBLE (newline suppression)
  - [ ] Commas and line continuation ignored inside quoted strings
  - [ ] BTW ignores trailing `...` and `,` (comment always terminates at real newline)
  - [ ] Report error for unterminated string literals
- [ ] Write lexer tests for all token types
- [ ] Write lexer edge case tests (partial multi-word keywords, `IM` as identifier, nested escapes)
- [ ] Write lexer error tests (unterminated strings, continuation before empty line, invalid escapes)
- [ ] Verify: can tokenize all sample programs

## Phase 2: Parser (AST Construction)
> Build an Abstract Syntax Tree from the token stream using recursive descent.
>
> **Depends on:** Phase 1

- [ ] Define AST node base classes (`SyntaxNode`, `StatementSyntax`, `ExpressionSyntax`)
- [ ] Define all statement node types
  - [ ] `CompilationUnitSyntax` (HAI version ... KTHXBYE)
  - [ ] `VariableDeclarationSyntax` (I HAS A)
  - [ ] `AssignmentSyntax` (R)
  - [ ] `PrintSyntax` (VISIBLE) — infinite arity, multiple expressions, `!` newline suppression flag
  - [ ] `InputSyntax` (GIMMEH)
  - [ ] `IfSyntax` (O RLY? / YA RLY / MEBBE / NO WAI / OIC)
  - [ ] `SwitchSyntax` (WTF? / OMG / OMGWTF / OIC) — OMG values must be literal tokens
  - [ ] `LoopSyntax` (IM IN YR / IM OUTTA YR)
  - [ ] `FunctionDeclarationSyntax` (HOW IZ I / IF U SAY SO)
  - [ ] `ReturnSyntax` (FOUND YR)
  - [ ] `BreakSyntax` (GTFO)
  - [ ] `CastSyntax` (IS NOW A)
  - [ ] `ExpressionStatementSyntax`
- [ ] Define all expression node types
  - [ ] `LiteralExpressionSyntax` (numbers, strings, WIN, FAIL)
  - [ ] `VariableExpressionSyntax` (identifiers, IT)
  - [ ] `BinaryExpressionSyntax` (SUM OF, DIFF OF, PRODUKT OF, QUOSHUNT OF, MOD OF, BIGGR OF, SMALLR OF)
  - [ ] `UnaryExpressionSyntax` (NOT)
  - [ ] `ComparisonExpressionSyntax` (BOTH SAEM, DIFFRINT)
  - [ ] `BooleanExpressionSyntax` (BOTH OF, EITHER OF, WON OF)
  - [ ] `NaryExpressionSyntax` (ALL OF, ANY OF) — terminated by MKAY or EOL
  - [ ] `CastExpressionSyntax` (MAEK ... [A] type)
  - [ ] `SmooshExpressionSyntax` (SMOOSH ... MKAY) — terminated by MKAY or EOL
  - [ ] `InterpolatedStringExpressionSyntax` (string with `:{var}` segments)
  - [ ] `FunctionCallExpressionSyntax` (I IZ ... MKAY) — MKAY may be omitted at EOL
- [ ] Implement `Parser` — recursive descent
  - [ ] Token consumption helpers (Match, Expect, Peek)
  - [ ] Error recovery (sync on newlines and commas)
  - [ ] Statement parsing
  - [ ] Expression parsing (prefix notation)
  - [ ] Handle optional `AN` separator between binary operator arguments
  - [ ] Handle `MKAY` omission at end of line/statement for variadic operators
  - [ ] Comma as statement separator
  - [ ] HAI version number parsing and validation
- [ ] Write parser tests for each language construct
- [ ] Write parser error recovery tests
- [ ] Verify: can parse all sample programs

## Phase 3: Binder & Lowering (Semantic Analysis)
> Validate semantics, resolve types, produce a bound tree, and lower complex constructs.
>
> **Depends on:** Phase 2

- [ ] Define type symbols (`TypeSymbol` for NUMBR, NUMBAR, YARN, TROOF, NOOB, TYPE)
- [ ] Define `VariableSymbol` and `FunctionSymbol`
- [ ] Implement `BoundScope` (nested scopes for variables/functions)
- [ ] Define bound node hierarchy (mirrors syntax nodes with type info)
- [ ] Implement `Binder`
  - [ ] Variable declaration and lookup
  - [ ] Function declaration and lookup
  - [ ] Enforce function scope isolation (no outer variable access)
  - [ ] Type inference from literals and expressions
  - [ ] Implicit type coercion rules (NOOB → TROOF only; YARN in math → parse as NUMBR/NUMBAR)
  - [ ] NOOB restriction enforcement (error on implicit cast except TROOF; warn when statically provable)
  - [ ] `IT` variable management per scope (see DESIGN.md §IT Variable Semantics)
  - [ ] Implicit `IT` return from functions (when no FOUND YR)
  - [ ] No automatic casting in equality comparisons (BOTH SAEM, DIFFRINT)
  - [ ] Boolean operators auto-cast operands to TROOF
  - [ ] Math type promotion: NUMBR+NUMBR=NUMBR, any NUMBAR→NUMBAR
  - [ ] Comparison type promotion: same as math for numeric types
  - [ ] NUMBAR → YARN truncation to 2 decimal places (casting rule)
  - [ ] `WTF?` validation: OMG values must be literals (no expressions, no interpolated strings)
  - [ ] `WTF?` validation: each OMG literal must be unique within the switch
  - [ ] `GTFO` context tracking: control-flow stack for loop/switch/function (see DESIGN.md §GTFO Context Sensitivity)
  - [ ] Loop iteration variable scoping (temporary, local to the loop)
  - [ ] Loop operation validation: UPPIN, NERFIN, or valid unary function name
  - [ ] `TYPE` type handling (bare word values, TYPE → TROOF casting, TYPE → YARN casting)
  - [ ] `BUKKIT` usage detection → produce error diagnostic
  - [ ] `GIMMEH` always stores as YARN type
  - [ ] `HAI` version number handling (accept 1.2, warn on others)
  - [ ] Semantic error reporting
- [ ] Implement `Lowerer` (simplifies bound tree for emitter)
  - [ ] `UPPIN`/`NERFIN` loops → while-loop with explicit increment/decrement
  - [ ] `SMOOSH` n-ary → chain of `String.Concat` calls
  - [ ] `ALL OF` / `ANY OF` → short-circuit chain of `BOTH OF` / `EITHER OF`
  - [ ] `BIGGR OF` / `SMALLR OF` → conditional branch or `Math.Max`/`Math.Min`
  - [ ] `VISIBLE` with multiple args → concatenate YARN-cast values + print
  - [ ] Interpolated strings → SMOOSH-equivalent concatenation
- [ ] Write binder tests (positive and negative cases)
- [ ] Write binder error tests (undeclared vars, type mismatches, NOOB misuse, BUKKIT usage)
- [ ] Create conformance test matrix (one test per LANGUAGE_SPEC section × positive × negative)
- [ ] Verify: all sample programs pass semantic analysis

## Phase 4: IL Emitter (Code Generation)
> Generate .NET IL from the bound tree and save as a runnable DLL.
>
> **Depends on:** Phase 3

- [ ] Create `Lolcode.Runtime` support library (runtime helpers)
  - [ ] `LolRuntime.IsTruthy(object)` → truthiness evaluation
  - [ ] `LolRuntime.Coerce(object, LolType)` → explicit casting with LOLCODE semantics
  - [ ] `LolRuntime.Add/Subtract/Multiply/Divide/Modulo` → type-aware arithmetic with NUMBR/NUMBAR promotion
  - [ ] `LolRuntime.Greater/Smaller` → type-aware BIGGR OF / SMALLR OF
  - [ ] `LolRuntime.Equal/NotEqual` → type-aware comparison (no auto-cast across types)
  - [ ] `LolRuntime.Concat(object[])` → SMOOSH implementation (cast all to YARN and join)
  - [ ] `LolRuntime.CastToYarn(object)` → to-string with NUMBAR 2-decimal truncation (`"F2"`)
  - [ ] `LolRuntime.Print(object[], bool)` → VISIBLE with infinite arity and newline suppression
  - [ ] `LolRuntime.ReadInput()` → GIMMEH wrapper (always returns YARN)
- [ ] Implement `Emitter` with `PersistedAssemblyBuilder`
  - [ ] Assembly and module setup
  - [ ] Entry point (`Main` method) generation
  - [ ] Local variable allocation (as `object` for dynamic typing)
  - [ ] Literal loading (ldstr, ldc.i4, ldc.r8, ldnull for NOOB)
  - [ ] Variable load/store (ldloc, stloc)
  - [ ] `IT` variable allocation and management per scope
  - [ ] `VISIBLE` → `LolRuntime.Print` (handles multiple args + `!` suppression)
  - [ ] `GIMMEH` → `LolRuntime.ReadInput` (stores as YARN)
  - [ ] Arithmetic → runtime helpers (or native IL opcodes when types statically known)
  - [ ] Comparison → runtime helpers (no auto-cast across types)
  - [ ] Boolean logic → IsTruthy + and/or/xor/not
  - [ ] Conditionals → brfalse/brtrue + labels
  - [ ] `WTF?` switch → labels per case with fall-through (no GTFO = continue to next OMG block)
  - [ ] Loops → labels + br (branch back); GTFO → br to loop exit label
  - [ ] Functions → DefineMethod + call; GTFO in function → ldnull + ret
  - [ ] String concatenation → `LolRuntime.Concat` or `String.Concat`
  - [ ] String interpolation → lowered to concatenation (handled by lowerer)
  - [ ] Type casting → conversion opcodes / runtime helpers
  - [ ] TYPE values → string representations
- [ ] Generate `.runtimeconfig.json` alongside DLL
- [ ] Reference `Lolcode.Runtime.dll` in output
- [ ] Implement `--emit-il` flag to dump human-readable IL for debugging
- [ ] Write emitter tests (compile → run → assert stdout)
- [ ] Write IL structure tests (verify labels, branches, call sites using `--emit-il` or ILSpy)
- [ ] Write coercion/casting test suite (every row in LANGUAGE_SPEC Casting Rules Summary)
- [ ] Write `IT` lifetime tests (IT after function call, IT in nested conditionals, IT in loops)
- [ ] Write `WTF?` fall-through tests (with and without GTFO)
- [ ] Verify: `dotnet <output>.dll` works for all sample programs

## Phase 5: CLI Tool (**MVP Complete**)
> Package the compiler as a usable command-line tool.
>
> **Depends on:** Phase 4

- [ ] Implement `lolcode compile <file.lol> [-o output.dll]`
- [ ] Implement `lolcode run <file.lol>` (compile + execute in temp)
- [ ] Pretty-print diagnostics with source context and colors
- [ ] Exit codes: 0 = success, 1 = compile error, 2 = runtime error
- [ ] Package as .NET global tool
- [ ] Write CLI integration tests
- [ ] Create all 15 sample programs (graduated complexity)
- [ ] Run conformance test suite: all samples compile and run with correct stdout/exit codes
- [ ] Write diagnostic snapshot tests (stable LOLxxxx IDs, error messages for common mistakes)
- [ ] Verify: all 15 sample programs compile and run correctly

---

> **MVP milestone reached after Phase 5.** Everything below is enhancement.

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

## Phase 7: MSBuild SDK Integration
> Enable `dotnet build` and `dotnet run` for `.lol` projects.
>
> **Depends on:** Phase 5 (compiler CLI must work)

- [ ] Create `Lolcode.Sdk` NuGet package
- [ ] Implement `Sdk.props` (default properties, item groups, framework refs)
- [ ] Implement `Sdk.targets` (CompileLolcode target with Inputs/Outputs for incremental builds)
- [ ] Implement `LolcodeCompileTask` (custom MSBuild task)
- [ ] Auto-reference `Lolcode.Runtime.dll` in output
- [ ] Generate `.runtimeconfig.json` in build targets
- [ ] Handle design-time builds gracefully (VS/VS Code)
- [ ] Support `<Project Sdk="Lolcode.Sdk">` in project files
- [ ] Test: `dotnet build` compiles `.lol` → `.dll`
- [ ] Test: `dotnet run` executes the program

## Phase 8: Debugging Support (Bonus)
> Enable VS Code debugging for LOLCODE programs.
>
> **Depends on:** Phase 4 (emitter) + Phase 6 (VS Code extension)

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
