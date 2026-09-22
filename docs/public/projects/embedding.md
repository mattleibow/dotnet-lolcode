# Embedding and scripting

Use `Lolcode.CodeAnalysis` when a managed host needs syntax, diagnostics, or
assembly bytes. Use `LolcodeScript` when it needs a higher-level in-memory
compile/run workflow with redirected input and output.

## Parse and inspect

```csharp
using Lolcode.CodeAnalysis;
using Lolcode.CodeAnalysis.Syntax;

var tree = SyntaxTree.ParseText("""
    HAI 1.2
      VISIBLE "OH HAI!"
    KTHXBYE
    """);
var compilation = LolcodeCompilation.Create(tree);
var diagnostics = compilation.GetDiagnostics();
```

Syntax trees and diagnostics are immutable public contracts. The binder,
lowerer, code generator, and library-discovery implementation classes remain internal.

## Emit to caller-owned streams

```csharp
using var pe = new MemoryStream();
using var pdb = new MemoryStream();
EmitResult result = compilation.Emit(pe, pdb);
```

Stream emission:

- accepts caller-owned writable PE and optional PDB streams;
- creates no files and returns no output/PDB paths;
- resolves the runtime from loaded compiler/runtime assemblies and needs no
  runtime DLL path;
- propagates PDB stream write failures to the caller;
- does not deploy runtime or referenced library assets.

A zero-tree executable compilation is valid. API hosts decide what to do with
the resulting assembly and its dependencies.

## Emit to paths

Path emission requires a runtime assembly path and coordinates the DLL,
optional PDB, and runtime configuration as one path operation. Staging and
commit protect existing outputs. If optional PDB staging or serialization
fails, the compiler can emit the PE without symbols and remove stale symbol
state. It does not deploy referenced assets; deployment belongs to
SDK/NuGet/project assets.

## Run as a script

`LolcodeScript` wraps compilation, collectible loading where available,
captured streams, cached PE/PDB bytes, diagnostics, return values, and runtime
exceptions. A browser or server host must still enforce its own resource and
security policy: `I DUZ`, `STDIO`, and `SOCKS` are host capabilities, not a
sandbox boundary.

Browse the [generated API](../api.md) for signatures and the
[compiler tooling chapter](../compiler-course/tooling.md) for architecture.
