# Roadmap

Build phases for the dotnet-lolcode compiler. Each phase builds on the previous one.

## Phase 0: Project Scaffolding
> Set up the solution structure, build configuration, and initial project files.

- [ ] Initialize git repo with `.gitignore`
- [ ] Create `dotnet-lolcode.sln`
- [ ] Create `Directory.Build.props` (net10.0, C# 14, nullable, implicit usings)
- [ ] Create `Lolcode.Compiler` class library project
- [ ] Create `Lolcode.Cli` console project
- [ ] Create `Lolcode.Compiler.Tests` xUnit test project
- [ ] Verify `dotnet build` and `dotnet test` work

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
  - [ ] Number literals (NUMBR and NUMBAR)
  - [ ] String literals with LOLCODE escape sequences
  - [ ] String interpolation `:{var}` handling
  - [ ] Unicode escapes `:(<hex>)` and `:[<name>]`
  - [ ] Multi-word keyword recognition with lookahead
  - [ ] Identifier recognition (case-sensitive)
  - [ ] Comment handling (BTW, OBTW/TLDR)
  - [ ] Line continuation (`...` and `…`)
  - [ ] Soft-command-break (`,`) as virtual newline
- [ ] Write lexer tests for all token types
- [ ] Verify: can tokenize all sample programs

## Phase 2: Parser (AST Construction)
> Build an Abstract Syntax Tree from the token stream using recursive descent.
>
> **Depends on:** Phase 1

- [ ] Define AST node base classes (`SyntaxNode`, `StatementSyntax`, `ExpressionSyntax`)
- [ ] Define all statement node types
  - [ ] `CompilationUnitSyntax` (HAI ... KTHXBYE)
  - [ ] `VariableDeclarationSyntax` (I HAS A)
  - [ ] `AssignmentSyntax` (R)
  - [ ] `PrintSyntax` (VISIBLE)
  - [ ] `InputSyntax` (GIMMEH)
  - [ ] `IfSyntax` (O RLY? / YA RLY / MEBBE / NO WAI / OIC)
  - [ ] `SwitchSyntax` (WTF? / OMG / OMGWTF / OIC)
  - [ ] `LoopSyntax` (IM IN YR / IM OUTTA YR)
  - [ ] `FunctionDeclarationSyntax` (HOW IZ I / IF U SAY SO)
  - [ ] `ReturnSyntax` (FOUND YR)
  - [ ] `BreakSyntax` (GTFO)
  - [ ] `CastSyntax` (IS NOW A)
  - [ ] `ExpressionStatementSyntax`
- [ ] Define all expression node types
  - [ ] `LiteralExpressionSyntax` (numbers, strings, WIN, FAIL)
  - [ ] `VariableExpressionSyntax` (identifiers, IT)
  - [ ] `BinaryExpressionSyntax` (SUM OF, DIFF OF, etc.)
  - [ ] `UnaryExpressionSyntax` (NOT)
  - [ ] `ComparisonExpressionSyntax` (BOTH SAEM, DIFFRINT)
  - [ ] `BooleanExpressionSyntax` (BOTH OF, EITHER OF, WON OF)
  - [ ] `NaryExpressionSyntax` (ALL OF, ANY OF)
  - [ ] `CastExpressionSyntax` (MAEK)
  - [ ] `SmooshExpressionSyntax` (SMOOSH)
  - [ ] `FunctionCallExpressionSyntax` (I IZ ... MKAY)
- [ ] Implement `Parser` — recursive descent
  - [ ] Token consumption helpers (Match, Expect, Peek)
  - [ ] Error recovery (sync on newlines)
  - [ ] Statement parsing
  - [ ] Expression parsing (prefix notation)
  - [ ] Comma as statement separator
- [ ] Write parser tests for each language construct
- [ ] Verify: can parse all sample programs

## Phase 3: Binder (Semantic Analysis)
> Validate semantics, resolve types, and produce a bound tree.
>
> **Depends on:** Phase 2

- [ ] Define type symbols (`TypeSymbol` for NUMBR, NUMBAR, YARN, TROOF, NOOB, TYPE, BUKKIT)
- [ ] Define `VariableSymbol` and `FunctionSymbol`
- [ ] Implement `BoundScope` (nested scopes for variables/functions)
- [ ] Define bound node hierarchy (mirrors syntax nodes with type info)
- [ ] Implement `Binder`
  - [ ] Variable declaration and lookup
  - [ ] Function declaration and lookup
  - [ ] Enforce function scope isolation (no outer variable access)
  - [ ] Type inference from literals and expressions
  - [ ] Implicit type coercion rules (NOOB → TROOF only; YARN in math → parse as NUMBR/NUMBAR)
  - [ ] NOOB restriction enforcement (error on implicit cast except TROOF)
  - [ ] `IT` variable management per scope
  - [ ] Implicit `IT` return from functions (when no FOUND YR)
  - [ ] No automatic casting in equality comparisons
  - [ ] Semantic error reporting
- [ ] Write binder tests
- [ ] Verify: all sample programs pass semantic analysis

## Phase 4: IL Emitter (Code Generation)
> Generate .NET IL from the bound tree and save as a runnable DLL.
>
> **Depends on:** Phase 3

- [ ] Create `Lolcode.Runtime` support library (runtime helpers)
  - [ ] `LolRuntime.IsTruthy(object)` → truthiness evaluation
  - [ ] `LolRuntime.Coerce(object, LolType)` → explicit casting
  - [ ] `LolRuntime.Add/Subtract/Multiply/Divide/Modulo` → type-aware arithmetic
  - [ ] `LolRuntime.Equal/NotEqual` → type-aware comparison
  - [ ] `LolRuntime.Concat(object[])` → SMOOSH implementation
  - [ ] `LolRuntime.Print(object, bool)` → VISIBLE with newline suppression
  - [ ] `LolRuntime.ReadInput()` → GIMMEH wrapper
- [ ] Implement `Emitter` with `PersistedAssemblyBuilder`
  - [ ] Assembly and module setup
  - [ ] Entry point (`Main` method) generation
  - [ ] Local variable allocation (as `object` for dynamic typing)
  - [ ] Static type specialization (use native opcodes when types are known)
  - [ ] Literal loading (ldstr, ldc.i4, ldc.r8)
  - [ ] Variable load/store (ldloc, stloc)
  - [ ] `VISIBLE` → `LolRuntime.Print` or `Console.WriteLine`
  - [ ] `GIMMEH` → `Console.ReadLine`
  - [ ] Arithmetic → runtime helpers or native IL opcodes
  - [ ] Comparison → runtime helpers (no auto-cast across types)
  - [ ] Boolean logic → and, or, xor, not
  - [ ] Conditionals → brfalse/brtrue + labels
  - [ ] Loops → labels + br (branch back)
  - [ ] Functions → DefineMethod + call
  - [ ] String concatenation → `LolRuntime.Concat` or `String.Concat`
  - [ ] String interpolation → expand `:{var}` at compile time
  - [ ] Type casting → conversion opcodes / runtime helpers
  - [ ] `BIGGR OF` / `SMALLR OF` → Math.Max / Math.Min
  - [ ] TYPE values → string representations
  - [ ] BUKKIT → Dictionary<string, object> operations
- [ ] Generate `.runtimeconfig.json` alongside DLL
- [ ] Reference `Lolcode.Runtime.dll` in output
- [ ] Write emitter tests (compile → run → assert stdout)
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
  - [ ] Type names (NUMBR, NUMBAR, YARN, TROOF, NOOB, TYPE, BUKKIT)
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

- [ ] Comprehensive README with getting started guide
- [ ] Ensure all docs are accurate and complete
- [ ] Add GitHub Actions CI/CD (build + test on push/PR)
- [ ] Create release workflow (NuGet + VSIX publishing)
- [ ] Record demo GIF for README
- [ ] All 15 samples verified working end-to-end
- [ ] Consider: REPL mode (`lolcode repl`)
- [ ] Consider: Language Server Protocol for rich VS Code features
- [ ] Consider: `--emit-il` flag to dump human-readable IL
