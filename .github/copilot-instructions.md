# Copilot Instructions for dotnet-lolcode

## Project Overview
This is a LOLCODE 1.2 compiler targeting .NET 10, written in C# 14. It compiles `.lol` source files to .NET IL assemblies using `PersistedAssemblyBuilder`.

## Architecture
Roslyn-inspired pipeline: Lexer → Parser → Binder → Lowerer → Emitter → DLL

## Project Structure
```
src/Lolcode.Compiler/     # Core compiler library (lexer, parser, binder, emitter)
src/Lolcode.Runtime/       # Runtime helper library (referenced by compiled programs)
src/Lolcode.Cli/           # CLI tool (lolcode compile/run)
tests/Lolcode.Compiler.Tests/  # xUnit tests for compiler
tests/                     # .lol/.txt conformance test pairs
samples/                   # 15 example programs
docs/                      # Design docs, language spec, roadmap
```

## Coding Conventions
- Target: `net10.0`, C# 14, nullable enabled, implicit usings
- Use records for immutable data (tokens, AST nodes, bound nodes)
- Use `ImmutableArray<T>` for collections in AST/bound tree
- All public APIs must have XML doc comments
- Each compiler phase must be independently testable
- Use `DiagnosticBag` for error collection across all phases
- Diagnostic IDs follow pattern `LOLxxxx` (e.g., LOL0001)

## Key Technical Decisions
- All LOLCODE variables are emitted as `System.Object` locals (dynamic typing via boxing)
- Runtime helpers in `Lolcode.Runtime` handle type coercion, arithmetic, etc.
- NUMBAR → YARN truncates to 2 decimal places (`value.ToString("F2")`)
- `IT` is a per-scope implicit variable (each function gets its own)
- `GTFO` is context-sensitive: breaks loop, breaks switch, or returns NOOB from function
- `PersistedAssemblyBuilder` (.NET 9+) is used for IL emission

## Testing
- Conformance tests: `.lol` + `.txt` pairs in `tests/` — compile, run, compare stdout
- Unit tests: xUnit + FluentAssertions in `tests/Lolcode.Compiler.Tests/`
- Use `ilspycmd` to inspect emitted IL when debugging emitter issues
- Build command: `dotnet build`
- Test command: `dotnet test`

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
