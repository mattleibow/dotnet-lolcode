# API reference

The generated [.NET API reference](xref:Lolcode.CodeAnalysis) is built from
the public compiler, runtime, provider, and MSBuild projects. It documents the
compiler surface, including `SyntaxTree`, `LolcodeCompilation`, diagnostics,
symbols, text spans, and the scripting API.

For an API-first path, parse with `SyntaxTree.ParseText`, create a compilation
with `LolcodeCompilation.Create`, inspect `GetDiagnostics`, then call `Emit`.
For in-memory execution, use `LolcodeScript`. The [compiler course](compiler-course/index.md)
explains why those public boundaries exist and where their internal work lives.
