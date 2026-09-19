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

`Emit` requires the path to `Lolcode.Runtime.dll`, which is available from the
[Lolcode.Runtime](https://www.nuget.org/packages/Lolcode.Runtime) package.

```csharp
var result = compilation.Emit("hello.dll", runtimeAssemblyPath);
if (!result.Success)
    throw new InvalidOperationException("LOLCODE compilation failed.");
```

## Compatibility

The package targets .NET 10 and follows the public API shape described in the
repository's [design documentation](https://github.com/mattleibow/dotnet-lolcode/blob/main/docs/dev/compiler-architecture.md).

## License

[MIT](https://github.com/mattleibow/dotnet-lolcode/blob/main/LICENSE)
