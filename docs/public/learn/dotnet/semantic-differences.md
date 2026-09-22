# Semantic differences that matter

## Dynamic values, NOOB, and conversion

Variables are dynamically valued and emitted through `System.Object`. NOOB is
represented by `null`, but do not import C# null semantics wholesale:

- NOOB converts to TROOF as `FAIL`;
- other implicit NOOB conversion is an error;
- explicit NOOB-to-YARN produces `""`;
- arithmetic on NOOB throws `LolRuntimeException`.

Explicit casts and operator coercion are separate contracts. Explicit numeric
casts accept documented numeric prefixes and produce zero for invalid YARN
input. Nonnumeric YARN arithmetic throws. NUMBAR display truncates toward zero
to exactly two decimals; it does not use normal .NET formatting rounding.

## Equality, switch, and `IT`

`BOTH SAEM` does not generally auto-cast operands. NUMBR/NUMBAR values compare
numerically, but `"3"` is not `3`. `WTF?` case identity uses exact runtime type
and value, and cases fall through unless `GTFO` exits.

Bare expression statements set `IT`; assignments do not. Each function owns
its own `IT`.

| Version | Conditional/switch scope | Loop scope and final `IT` |
| --- | --- | --- |
| 1.2 | declarations and `IT` remain in enclosing scope | each iteration has a child scope; final `IT` propagates, and zero iterations preserve the prior `IT` |
| 1.3/1.4 | body child scopes; declarations and `IT` do not leak | body child scopes; declarations and `IT` do not leak |

## Loops and functions

Loop counters are fresh NUMBR values initialized to zero, even when an outer
variable has the same name or a desired starting value. The guard runs before
the body, then the operation runs after it. `GTFO` skips the operation.

Normal functions cannot capture outer variables or form CLR/F# closures. Pass
dependencies as parameters. Dynamic 1.3 calls can resolve caller lexical
bindings, but object receiver slots still require `ME'Z`. A receiverless call
preserves ambient `ME`.

## Objects and initialization

BUKKITs are prototype objects with dynamic slots, identity equality, and
receiver-bound `ME`; they are not CLR classes. Multi-file direct top-level
functions are hoisted before normal initialization when a compilation has more
than one syntax tree. Dynamic/SRS functions, nested functions, and object
methods are not hoisted. Single-file 1.3/1.4 textual replacement remains
dynamic.

Variables, imports, I/O, and side effects preserve `Compile` order. Treat item
order more like F# file order or a module initializer sequence than C# source
file order.

Next: [projects and interoperability](workflows-and-interop.md).
