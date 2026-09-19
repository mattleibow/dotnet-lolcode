---
title: Build a .NET language compiler
---
# Build your own .NET language compiler

This course follows the actual dotnet-lolcode implementation. It is not a
generic diagram: every chapter names files to open, explains the phase's job,
and ends with a checkpoint. Build the repository first:

```bash
git clone --recurse-submodules https://github.com/mattleibow/dotnet-lolcode.git
cd dotnet-lolcode
dotnet build
dotnet test
```

The compiler is intentionally Roslyn-inspired: source text -> lexer -> parser
-> binder -> lowerer -> IL generator -> assembly. The public entry point is
<xref:Lolcode.CodeAnalysis.LolcodeCompilation>;
the internal stages remain testable through the test assembly.

1. [Repository and pipeline](pipeline.md)
2. [Source text, tokens, and parsing](syntax.md)
3. [Diagnostics, symbols, and binding](binding.md)
4. [Bound tree, lowering, runtime, and IL](runtime-il.md)
5. [Assemblies, scripts, and tooling](tooling.md)
6. [Playground, testing, conformance, and your next language](quality-roadmap.md)

Cross-check behavior against the [implementation profile](../language/implementation.md)
and run an example from the [sample gallery](../language/samples.md) as you
trace it.
