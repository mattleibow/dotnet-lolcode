# API reference

Use this page to choose the right public surface before entering the generated
[.NET API reference](xref:Lolcode.CodeAnalysis).

- **Lolcode.CodeAnalysis** provides immutable syntax trees, diagnostics,
  symbols, compilations, stream/path emission, and the scripting host. Start
  with `SyntaxTree.ParseText`, `LolcodeCompilation.Create`, `GetDiagnostics`,
  and `Emit`.
- **Lolcode.Runtime** contains the public runtime contracts used by emitted
  programs and eligible managed libraries, including scopes, objects,
  functions, BLOB ownership, and `[LolcodeLibrary]`.
- **Lolcode.Build** exposes the `Lolc` MSBuild task and identifier-normalization
  task used by `Lolcode.NET.Sdk`.

Provider implementation classes are internal details of their packages and are
not promised as generated public API. User-facing provider behavior belongs in
the [library documentation](language/libraries.md). For in-memory execution,
use `LolcodeScript`; for architecture and internals, follow the
[compiler course](compiler-course/index.md).
