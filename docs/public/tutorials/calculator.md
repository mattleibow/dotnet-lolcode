# Calculator: input, functions, and cases

The [string calculator sample](../../../samples/programs/string-calculator/calculator.lol)
is an interactive application with a clear boundary: `calc` receives two
numbers and an operator, converts values to NUMBAR, selects a calculation, and
returns the result through `FOUND YR`.

Its main loop asks for three YARN inputs. The word `quit` is checked *before*
numeric conversion, then the function converts operands and uses `WTF?` for
`+`, `-`, `*`, and `/`. The division case handles zero explicitly rather than
asking runtime arithmetic to explain it.

```lolcode
HOW IZ I calc YR a AN YR op AN YR b
  a IS NOW A NUMBAR
  b IS NOW A NUMBAR
  op
  WTF?
    OMG "+"
      FOUND YR SUM OF a AN b
      GTFO
    OMG "*"
      FOUND YR PRODUKT OF a AN b
      GTFO
  OMGWTF
    VISIBLE "DUNNO WAT " op " MEANZ"
    FOUND YR 0
  OIC
IF U SAY SO
```

Run it with `3`, `*`, and `4`, then with `quit`. Your next steps: complete the
other operations, show an error for nonnumeric input, or add a help command.
This tutorial meets the [reference](../language/reference.md) at casts,
functions, `WTF?`, and `GIMMEH`.
