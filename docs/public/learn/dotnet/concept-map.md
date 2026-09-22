# Concept map for .NET developers

Use these as starting analogies, not equivalences:

| LOLCODE | Useful .NET analogy | Important difference |
| --- | --- | --- |
| value | boxed `object` | runtime kind determines operations; source has no static variable type |
| `I HAS A` | local declaration | uninitialized means NOOB (`null` representation), not a definite-assignment error |
| `R` | assignment | assignment does not set `IT` |
| bare expression | expression statement | its result updates per-scope `IT` |
| `SUM OF a AN b` | `a + b` | prefix syntax and runtime numeric coercion |
| `MAEK` / `IS NOW A` | conversion / reassignment | explicit invalid numeric casts follow prefix/zero rules |
| `O RLY?` | `if` | consumes `IT`, not a parenthesized condition |
| `WTF?` | switch | exact runtime type/value cases and fallthrough without `GTFO` |
| `HOW IZ I` | function | isolated from outer program variables |
| BUKKIT | prototype object | not a CLR class, record, module, or nominal type |
| `.lolproj` | SDK-style project | `Compile` item order is semantically observable |
| `CAN HAS` | library import | constructs an attributed library instance and its BUKKIT |

## Expressions and results

LOLCODE operators precede arguments:

```lolcode
SUM OF PRODUKT OF 6 AN 7 AN 1
```

is equivalent in grouping to `(6 * 7) + 1`. `AN` between binary arguments is
optional in the grammar but keeping it improves readability.

`IT` is closer to an implicit last-result slot than to a C# discard, VB
implicit return variable, or F# pipeline value. Bare expressions and calls can
replace it; assignment does not. Its control-flow lifetime changes by language
version, described in [semantic differences](semantic-differences.md).

## Compilation and projects

`SyntaxTree.ParseText` and `LolcodeCompilation.Create` deliberately resemble
Roslyn. A multi-file project still emits one assembly. Every selected file is
a complete compilation unit; direct top-level functions are hoisted across
multiple trees, while imports, variables, side effects, and normal
initialization retain project item order.

Continue to [semantic differences](semantic-differences.md), then choose your
source language.
