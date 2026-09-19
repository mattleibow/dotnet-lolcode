# Repository and pipeline

A compiler needs phases because raw characters are not yet a program. Each
phase turns an ambiguous representation into one with stronger guarantees, so
later phases can be simple and diagnostics can identify the right concern.

| Phase | Repository implementation | Why it exists |
| --- | --- | --- |
| Text and lexing | `Text/SourceText.cs`, `Syntax/Lexer.cs` | Preserve locations and turn characters into tokens. |
| Parsing | `Syntax/Parser.cs`, `Syntax/SyntaxNodes.cs` | Give tokens grammatical tree structure. |
| Binding | `Binding/Binder.cs`, `Binding/BoundScope.cs` | Resolve names, validate rules, and select semantics. |
| Lowering | `Lowering/Lowerer.cs` | Current identity/tree-rewrite scaffold; keeps a future desugaring boundary. |
| Generation | `CodeGen/CodeGenerator.cs` | Write CIL for a real .NET assembly. |

`LolcodeCompilation` owns one or more syntax trees, aggregates diagnostics,
and invokes the internal pipeline. Tree order is preserved for top-level
initialization; functions are declared across the compilation before generation.
The library targets `net10.0`; `Lolcode.Runtime` centralizes dynamic
operations while compiler locals are emitted as `System.Object`. Runtime
identifier paths preserve SRS and BUKKIT lookup segments for evaluation rather
than falsely treating every name as static.

**Checkpoint:** Start at `LolcodeCompilation.GetDiagnostics`, then use
Find References through its binder call and emission path. Draw the call chain
for one `VISIBLE` program. Next, inspect [text and syntax](syntax.md).
