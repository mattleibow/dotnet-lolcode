# Bound tree, runtime semantics, and IL

`CodeGen/CodeGenerator.cs` takes the currently rewritten structured bound nodes and emits CIL with
`PersistedAssemblyBuilder`. A compiler output here is a normal managed assembly,
not C# source or an interpreter loop. The generator directly emits branches
for structured flow today; lowering has not yet reduced it to a label/goto
tree. Generated methods call helpers rather than duplicating every dynamic
operation.

`src/Lolcode.Runtime/LolRuntime.cs` owns coercion, arithmetic, comparison, I/O,
and LOLCODE's display semantics. Centralizing these rules matters because all
variables become `System.Object` locals. For example, NUMBAR-to-YARN conversion truncates toward zero to the documented
two-decimal representation, and invalid NOOB/non-numeric arithmetic
raises the runtime's defined error behavior. The [implementation profile](../language/implementation.md)
is the contract to preserve when changing either layer.

`LolcodeCompilation.Emit` creates a PE and, where appropriate, a PDB.
Caller-owned stream emission creates no files, accepts writable PE and optional
PDB streams, resolves the already loaded runtime without a runtime DLL path,
and propagates PDB write failures. A zero-tree executable is valid.

Path emission stages and coordinates DLL, optional PDB, and
`.runtimeconfig.json` output. If optional PDB staging or serialization fails,
it can still commit a PE without symbols and remove stale PDB state. Library
output gets a library header and a public sealed `IDisposable` instance type
with direct-top-level public methods instead of an entry point. Neither path
nor stream emission deploys referenced assets; SDK, NuGet, and project assets
own deployment.

**Exercise:** Find the code-generation branch for a `VisibleStatementSyntax`
after it becomes bound/lowered. Identify the `LolRuntime` helper it calls and
write an end-to-end test that asserts stdout, not its private IL sequence.
