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

`Lowering/Lowerer.cs` rewrites bound constructs into simpler blocks, labels,
gotos, and statements. Lowering keeps the source-level bound tree readable
while giving IL emission a deliberately small set of cases. `GTFO` is decided
with its context during semantic work, not guessed while writing opcodes.

**Checkpoint:** Trace an undefined variable from Binder reporting through
`LolcodeCompilation.GetDiagnostics`. Then find a lowered loop and identify
which source-level convenience it eliminates. Consult [diagnostics](../language/diagnostics.md)
for the learner-facing result.
