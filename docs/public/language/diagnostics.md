# Diagnostics

Compiler diagnostics have `LOLxxxx` IDs and a location. IDs are grouped by
phase: lexer errors are `LOL0xxx`, parser errors `LOL1xxx`, binder errors
`LOL2xxx`, and internal errors `LOL9xxx`. A build can return several messages;
fix the earliest structural problem first because later parsing can recover
around it.

The catalog is implemented in
[`Errors/ErrorCode.cs`](../../../src/Lolcode.CodeAnalysis/Errors/ErrorCode.cs) and
[`Errors/DiagnosticDescriptors.cs`](../../../src/Lolcode.CodeAnalysis/Errors/DiagnosticDescriptors.cs).
`Diagnostic`, `DiagnosticDescriptor`, and `DiagnosticBag` carry phase output
through the compiler.

For a first repair, check the block boundary (`OIC`, `IM OUTTA YR`, or `IF U SAY
SO`), then spelling and operands. A semantic message generally means the text
parsed but the program is invalid: a missing variable, wrong function
arguments, invalid cast, or inappropriate `GTFO`. Return to
[tooling and first errors](../getting-started/tooling.md) for a practical loop.
