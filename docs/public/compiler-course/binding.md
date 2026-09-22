# Diagnostics, symbols, scopes, binding, and lowering

Parsing answers "is this shaped like language syntax?" Binding answers "does it
mean a valid program?" `DiagnosticBag`, `Diagnostic`, and
`DiagnosticDescriptor` carry errors across phases. The catalog in
`Errors/ErrorCode.cs` and `Errors/DiagnosticDescriptors.cs` keeps stable
`LOLxxxx` identities instead of scattering message strings.

`Symbols/` models `Symbol`, `VariableSymbol`, `ParameterSymbol`,
`FunctionSymbol`, and `TypeSymbol`. `BoundScope` is a nested name table;
`Binder` uses it to resolve variables and functions, enforce function scope,
check casts and control flow, and produce the typed structures in
`BoundTree/BoundNodes.cs`. This means the generator never needs to rediscover
what an identifier means.

`Lowering/Lowerer.cs` is currently an identity/tree-rewrite scaffold. It keeps
the phase boundary available for future transformations, but does not yet turn
structured control flow into labels and gotos. `CodeGenerator` directly emits
the structured bound nodes and the necessary IL branches. `GTFO` is decided
with its context during semantic work, not guessed while writing opcodes.

For 1.3 and 1.4 features, binding preserves identifier paths for the runtime:
SRS segments are evaluated as YARN names, and BUKKIT slots, functions, and
variables share dynamic scope-aware bindings. Static checks still catch names
that can be known at compile time; runtime lookup is used only where a program
asks for a runtime name.

Scope is versioned behavior, not one universal “block scope” rule. In 1.2,
conditional and switch declarations plus `IT` remain in the enclosing
program/function scope. Loop iterations use child scopes, propagate their final
`IT`, and preserve the previous `IT` when the body never runs. In 1.3/1.4,
conditional, switch, and loop bodies are child scopes whose declarations and
`IT` do not leak. Dynamic calls can resolve caller lexical bindings;
receiverless calls preserve ambient `ME`, while receiver slots require
`ME'Z`.

**Checkpoint:** Trace an undefined variable from Binder reporting through
`LolcodeCompilation.GetDiagnostics`. Then follow a loop from its bound node
into `CodeGenerator` and identify the emitted branch points. Consult [diagnostics](../language/diagnostics.md)
for the learner-facing result.
