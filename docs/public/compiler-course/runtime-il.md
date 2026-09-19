# Bound tree, runtime semantics, and IL

`CodeGen/CodeGenerator.cs` takes lowered nodes and emits CIL with
`PersistedAssemblyBuilder`. A compiler output here is a normal managed assembly,
not C# source or an interpreter loop. Labels and branches represent lowered
flow; generated methods call helpers rather than duplicating every dynamic
operation.

`src/Lolcode.Runtime/LolRuntime.cs` owns coercion, arithmetic, comparison, I/O,
and LOLCODE's display semantics. Centralizing these rules matters because all
variables become `System.Object` locals. For example, NUMBAR display uses the
documented two-decimal representation, and invalid NOOB/non-numeric arithmetic
raises the runtime's defined error behavior. The [implementation profile](../language/implementation.md)
is the contract to preserve when changing either layer.

`LolcodeCompilation.Emit` creates a PE and, where appropriate, a PDB. Its
path-based overload coordinates DLL, PDB, and `.runtimeconfig.json` writing so
a runnable output has compatible runtime metadata.

**Exercise:** Find the code-generation branch for a `VisibleStatementSyntax`
after it becomes bound/lowered. Identify the `LolRuntime` helper it calls and
write an end-to-end test that asserts stdout, not its private IL sequence.
