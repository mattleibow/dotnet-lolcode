# Lolcode.CodeAnalysis

`Lolcode.CodeAnalysis` is a Roslyn-inspired API for parsing, analyzing, and
emitting LOLCODE programs that target .NET.

Use this package when you are building tooling around LOLCODE source—for example,
an analyzer, editor integration, source generator, or custom build workflow. To
build ordinary LOLCODE applications, use
[Lolcode.NET.Sdk](https://www.nuget.org/packages/Lolcode.NET.Sdk) instead.

## Installation

```bash
dotnet add package Lolcode.CodeAnalysis
```

## Parse and analyze source

```csharp
using System;
using Lolcode.CodeAnalysis;
using Lolcode.CodeAnalysis.Syntax;

var tree = SyntaxTree.ParseText("""
    HAI 1.2
      VISIBLE "OH HAI!"
    KTHXBYE
    """);

var compilation = LolcodeCompilation.Create(tree);
foreach (var diagnostic in compilation.GetDiagnostics())
    Console.Error.WriteLine(diagnostic);
```

## Emit an assembly

Path-based `Emit` requires the path to `Lolcode.Runtime.dll`, which is available
from the [Lolcode.Runtime](https://www.nuget.org/packages/Lolcode.Runtime)
package. The public stream overload resolves the runtime from the currently
loaded compiler/runtime assemblies and supports hosts that load them from
memory or a bundle. Path-based emission writes only the PE, optional PDB, and
runtime configuration; callers deploying directly with this API must copy
runtime and provider dependencies from their resolved assets. SDK, NuGet, and
project-reference builds deploy those assets normally.

```csharp
var result = compilation.Emit("hello.dll", runtimeAssemblyPath);
if (!result.Success)
    throw new InvalidOperationException("LOLCODE compilation failed.");
```

## Compatibility

The package targets .NET 10 and follows the public API shape described in the
repository's [design documentation](https://github.com/mattleibow/dotnet-lolcode/blob/main/docs/DESIGN.md).

## License

[MIT](https://github.com/mattleibow/dotnet-lolcode/blob/main/LICENSE)
