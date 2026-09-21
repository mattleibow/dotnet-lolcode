# Language tour

LOLCODE programs are delimited by `HAI` and `KTHXBYE`. Values include NUMBR,
NUMBAR, YARN, TROOF, and NOOB. Bare expression statements set the implicit
`IT` value; assignments do not. `O RLY?` and `WTF?` consume `IT`.

```lolcode
HAI 1.2
  I HAS A cats ITZ 3
  BOTH SAEM cats AN 3
  O RLY?
    YA RLY, VISIBLE "CATZ!"
    NO WAI, VISIBLE "MOAR CATZ?"
  OIC
KTHXBYE
```

Use `I HAS A`/`R` for variables, `GIMMEH`/`VISIBLE` for terminal I/O, prefix
forms such as `SUM OF` for arithmetic, and `MAEK` or `IS NOW A` for casts.
Loops use `IM IN YR` / `IM OUTTA YR`; functions use `HOW IZ I` / `IF U SAY SO`
and return with `FOUND YR`. `WTF?` chooses literal `OMG` cases and falls
through unless `GTFO` exits.

This is orientation, not a complete grammar. Follow the [learning route](../learn/index.md)
for programming ideas, or open the [reference hub](reference.md) when writing
real code. `IT` is per function/scope, but control-flow propagation differs
between the 1.2 and 1.3/1.4 profiles; see
[Everyday runtime behavior](implementation.md).
