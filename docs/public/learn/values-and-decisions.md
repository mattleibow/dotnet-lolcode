# Values and decisions: input, casts, math, and branches

`GIMMEH name` reads a line into `name` as YARN. Programs must choose how to
interpret input. `MAEK` produces a converted value; `IS NOW A` replaces one:

```lolcode
I HAS A answer
GIMMEH answer
answer IS NOW A NUMBR
VISIBLE SUM OF answer AN 1
```

Only cast text you expect to be numeric. Read the full [casting sample](../../../samples/basics/casting/casting.lol)
for display-safe TROOF conversion.

Arithmetic operators put their operands after `OF`: `SUM OF`, `DIFF OF`,
`PRODUKT OF`, `QUOSHUNT OF`, and `MOD OF`. Nest expressions to make a
calculation; `SUM OF PRODUKT OF 3 AN 4 AN 5` is `(3 * 4) + 5`.

Comparisons answer with TROOF. `BOTH SAEM` tests equality without automatic
cross-type conversion; `DIFFRINT` tests inequality. Boolean operators (`NOT`,
`BOTH OF`, `EITHER OF`, `WON OF`, `ALL OF`, `ANY OF`) work with truthiness.
Use the result in `IT`, then branch:

```lolcode
I HAS A score ITZ 100
BOTH SAEM score AN 100
O RLY?
  YA RLY
    VISIBLE "PERFEKT!"
  NO WAI
    VISIBLE "KEEP TRYIN"
OIC
```

`O RLY?` consumes the preceding expression's result. `MEBBE` offers another
condition. See the [math](../../../samples/basics/math/math.lol) and
[conditionals](../../../samples/basics/conditionals/conditionals.lol) samples.

**Checkpoint:** Ask for a number, convert it, and say whether it is even using
`MOD OF`. Then proceed to [loops and text](repetition-and-text.md).
