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

The built-in library classes are public because the runtime discovers the same
attributed instance contract used by custom libraries. Use the
[library documentation](language/libraries.md) for their LOLCODE slots and
behavior; use the generated API pages when calling the CLR types directly. For
in-memory execution, use `LolcodeScript`; for architecture and internals,
follow the [compiler course](compiler-course/index.md).
