# API reference

The generated [.NET API reference](api/index.md) is built from
`src/Lolcode.CodeAnalysis/Lolcode.CodeAnalysis.csproj`. It documents the public
surface, including `SyntaxTree`, `LolcodeCompilation`, diagnostics, symbols,
text spans, and the scripting API.

For an API-first path, parse with `SyntaxTree.ParseText`, create a compilation
with `LolcodeCompilation.Create`, inspect `GetDiagnostics`, then call `Emit`.
For in-memory execution, use `LolcodeScript`. The [compiler course](compiler-course/index.md)
explains why those public boundaries exist and where their internal work lives.
