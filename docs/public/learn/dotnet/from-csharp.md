# Coming from C#

## Resist the surface similarities

`I HAS A x ITZ 1` resembles `var x = 1`, but LOLCODE `x` has no inferred static
type. It can later hold a YARN or BUKKIT. Runtime operations, not overload
resolution, decide what happens.

| C# intuition | LOLCODE correction |
| --- | --- |
| `null` propagates through reference operations | NOOB has language-specific truth and conversion rules; arithmetic throws |
| `a + b` | prefix `SUM OF a AN b` |
| expression statements may discard a value | bare expressions replace `IT` |
| assignment is an expression | `R` assignment does not update `IT` |
| methods can read fields and captured locals | normal functions cannot read outer program variables |
| classes define nominal shapes | BUKKITs use dynamic slots and prototypes |
| `switch` does not fall through | `WTF?` falls through unless `GTFO` |
| source-file order is mostly irrelevant | multi-file initialization preserves `Compile` order |

## Classes and methods

Do not translate every class into `O HAI IM`. Stable 1.2 programs often work
best with explicit state variables plus pure functions. When using the 1.3
object progression, access receiver state through `ME'Z`; a bare name follows
lexical lookup instead.

For interop, expose small public static adapter methods. An overload group is
not imported, and inherited static methods are ignored because only methods
declared directly on the selected type participate.

## Example translation

C#:

```csharp
static string Label(int score) => score >= 60 ? "PASS" : "RETRY";
```

LOLCODE:

```lolcode
HOW IZ I label YR score
  BOTH SAEM BIGGR OF score AN 60 AN score
  O RLY?
    YA RLY, FOUND YR "PASS"
    NO WAI, FOUND YR "RETRY"
  OIC
IF U SAY SO
```

The LOLCODE function takes every dependency explicitly and returns from both
branches. Continue to [port a program](port-a-program.md).
