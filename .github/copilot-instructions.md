# Copilot Instructions for dotnet-lolcode

## Project Overview
This is a LOLCODE 1.2 compiler targeting .NET 10, written in C# 14. It compiles `.lol` source files to .NET IL assemblies using `PersistedAssemblyBuilder`.

## Architecture
Roslyn-inspired pipeline: Lexer → Parser → Binder → Lowerer → CodeGenerator → DLL

## Project Structure
```
src/Lolcode.CodeAnalysis/     # Core compiler library
├── Binding/                   # Binder, BoundScope
├── BoundTree/                 # Bound node types, BoundKind, operator enums
├── CodeGen/                   # CodeGenerator (IL emission)
├── Errors/                    # ErrorCode, DiagnosticDescriptors
├── Lowering/                  # Lowerer (tree rewriting)
├── Symbols/                   # Symbol, TypeSymbol, VariableSymbol, FunctionSymbol
├── Syntax/                    # SyntaxTree, SyntaxFacts, Lexer, Parser, syntax nodes
└── Text/                      # SourceText, TextSpan, TextLocation
src/Lolcode.Runtime/           # Runtime helper library (referenced by compiled programs)
src/Lolcode.Build/             # MSBuild task (Lolc) for SDK integration
src/Lolcode.NET.Sdk/           # MSBuild SDK package (Sdk.props, Sdk.targets)
src/Lolcode.NET.Templates/    # dotnet new template pack (lolcode-console)
tests/Lolcode.CodeAnalysis.Tests/  # xUnit tests for compiler
tests/                         # .lol/.txt conformance test pairs (18 categories, 117 pairs)
samples/                       # 15 example programs + SDK samples
docs/                          # Design docs, language spec, roadmap
```

## Coding Conventions
- Target: `net10.0`, C# 14, nullable enabled, implicit usings
- Use records for immutable data (tokens, AST nodes, bound nodes)
- Use `ImmutableArray<T>` for collections in AST/bound tree
- All public APIs must have XML doc comments
- Each compiler phase must be independently testable
- Use `DiagnosticBag` for error collection across all phases
- Diagnostic IDs follow pattern `LOLxxxx` (e.g., LOL0001)
- Lexer diagnostics: LOL0xxx, Parser: LOL1xxx, Binder: LOL2xxx, Internal: LOL9xxx

## Roslyn-Aligned API
The compiler API mirrors Roslyn's structure:
- `SyntaxTree.ParseText(source)` — parse source into an immutable syntax tree
- `LolcodeCompilation.Create(tree)` — create a compilation from syntax trees
- `compilation.GetDiagnostics()` — get all syntax + semantic diagnostics
- `compilation.Emit(outputPath, runtimePath)` — emit to DLL, returns `EmitResult`
- `SyntaxFacts` — centralized keyword/token utilities
- `Symbol` hierarchy — `VariableSymbol`, `FunctionSymbol`, `ParameterSymbol`, `TypeSymbol`
- `DiagnosticDescriptor` + `DiagnosticDescriptors` catalog — Roslyn-style diagnostic definitions
- `ErrorCode` enum — all LOLxxxx diagnostic codes
- Implementation types (Lexer, Parser, Binder, CodeGenerator, Lowerer, bound tree) are `internal`
- Tests access internals via `[InternalsVisibleTo("Lolcode.CodeAnalysis.Tests")]`

## Key Technical Decisions
- All LOLCODE variables are emitted as `System.Object` locals (dynamic typing via boxing)
- Runtime helpers in `Lolcode.Runtime` handle type coercion, arithmetic, etc.
- NUMBAR → YARN truncates to 2 decimal places (`value.ToString("F2", CultureInfo.InvariantCulture)`)
- `IT` is a per-scope implicit variable (each function gets its own)
- `GTFO` is context-sensitive: breaks loop, breaks switch, or returns NOOB from function
- `PersistedAssemblyBuilder` (.NET 9+) is used for IL emission
- `.runtimeconfig.json` is generated alongside the output DLL
- `Lolcode.Runtime.dll` must be copied next to output DLL for execution

## Runtime Error Handling
- NOOB in arithmetic operations throws `LolRuntimeException` (per spec)
- Non-numeric YARN in arithmetic throws `LolRuntimeException`
- Division by zero: integer returns 0, float returns Infinity/NaN

## Testing
- Conformance tests: `.lol` + `.txt` pairs in `tests/` — compile, run, compare stdout
- Unit tests: xUnit + FluentAssertions in `tests/Lolcode.CodeAnalysis.Tests/`
- End-to-end tests: compile LOLCODE source, run output DLL, assert stdout
- Use `ilspycmd` to inspect emitted IL when debugging emitter issues
- Use `--emit-il` or `--emit-csharp` CLI flags for quick IL inspection
- Build command: `dotnet build`
- Test command: `dotnet test`

## Usage
```
# File-based (no project needed)
dotnet run --file hello.lol     # Requires #:sdk Lolcode.NET.Sdk at top
dotnet hello.lol                # Shorthand

# Project-based
dotnet new lolcode -n MyApp     # Create .lolproj project
dotnet build                    # Compile .lol → .dll
dotnet run                      # Compile and execute
```

## Language Spec Reference
- Full spec: `docs/LANGUAGE_SPEC.md`
- Design details: `docs/DESIGN.md`
- Roadmap: `docs/ROADMAP.md`

## Important LOLCODE 1.2 Rules
- VISIBLE adds newline by default; `!` suppresses it
- VISIBLE has infinite arity (auto-concatenates args after YARN cast)
- NUMBAR printed = 2 decimal places (e.g., 7.0 → "7.00")
- BOTH SAEM has NO auto-casting: `BOTH SAEM "3" AN 3` is FAIL
- Boolean operators auto-cast operands to TROOF
- WTF?/OMG: literals only, unique, falls through without GTFO
- Loop variable (`UPPIN YR i`) is temporary and local to the loop
- Functions cannot access outer scope variables
- NOOB → TROOF (implicit) = FAIL; NOOB → other (implicit) = error
- String escapes: `:` is ALWAYS escape prefix in strings. `:)` = newline, `:>` = tab, `::` = literal colon, `:"` = quote, `:o` = bell
- String interpolation: `:{varname}` embeds variable value in string
- `AN` keyword is optional between binary operator arguments
- `MKAY` is optional at end of line for variadic operators

## MSBuild SDK Integration
- `Lolcode.NET.Sdk` — MSBuild SDK package for `.lolproj` projects
- `Lolcode.Build.Lolc` — MSBuild task that invokes `LolcodeCompilation` in-process
- `Sdk.props` imports `Microsoft.NET.Sdk`, sets `Language=LOLCODE`, globs `**/*.lol`
- `Sdk.targets` overrides `CoreCompile` with `Lolc` task, auto-references `Lolcode.Runtime.dll`
- `SkipCompilerExecution` property supports VS/VS Code design-time builds
- `pack-local.sh` builds a local `.nupkg` for testing SDK changes
- `local-dev-workflow.md` documents the full local SDK dev loop

## File-Based Apps
- `dotnet run --file hello.lol` works with `#:sdk Lolcode.NET.Sdk` at top of file
- `dotnet hello.lol` shorthand also works
- Lexer skips `#:` directives and `#!` shebang lines (treated as trivia)
- `Sdk.targets` filters `@(Compile)` to `.lol` files only (prevents auto-generated .cs contamination)
- `Sdk.props` sets `ImplicitUsings=disable` to suppress GlobalUsings.g.cs

## Known Limitations
- TYPE type (bare word type values) is deferred — spec says "under current review"
- BUKKIT (arrays/objects) is LOLCODE 1.3, not implemented
- SRS (dynamic identifiers) is LOLCODE 1.3, not implemented
- Custom loop operations (function as loop op) not yet implemented
- macOS parallel builds may have incremental build race conditions — retry or use `dotnet build` before `dotnet test`
