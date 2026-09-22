# Build an interactive calculator

**You need:** input, casts, functions, and `WTF?`.

**You will build:** a stable 1.2 calculator that reads two numbers and an
operator, handles division by zero, and repeats until `quit`.

## 1. Define the calculation boundary

Keep terminal input in the main program. Give a function three explicit
inputs—left operand, operator, right operand—and one result:

```lolcode
HOW IZ I calc YR a AN YR op AN YR b
  a IS NOW A NUMBAR
  b IS NOW A NUMBAR
  FOUND YR 0
IF U SAY SO
```

At this stage, call it with fixed values and prove the function wiring works.

## 2. Select an operator

Put `op` into `IT`, then use literal switch cases:

```lolcode
op
WTF?
  OMG "+"
    FOUND YR SUM OF a AN b
  OMG "-"
    FOUND YR DIFF OF a AN b
  OMG "*"
    FOUND YR PRODUKT OF a AN b
  OMGWTF
    VISIBLE "DUNNO WAT " op " MEANZ"
    FOUND YR 0
OIC
```

`FOUND YR` leaves the function, so these successful cases do not depend on
fallthrough. Add `/` separately because zero needs a policy.

## 3. Guard division

```lolcode
OMG "/"
  BOTH SAEM b AN 0.0
  O RLY?
    YA RLY
      VISIBLE "CANT DIVIDE BY ZERO"
      FOUND YR 0
    NO WAI
      FOUND YR QUOSHUNT OF a AN b
  OIC
```

This runtime throws for division/modulo by zero; the program gives a friendlier
message before the operation.

## 4. Add the interaction loop

```lolcode
I HAS A done ITZ FAIL
IM IN YR calculator WILE NOT done
  I HAS A a
  VISIBLE "FIRST NUMBER OR quit:: " !
  GIMMEH a

  BOTH SAEM a AN "quit"
  O RLY?
    YA RLY
      done R WIN
      GTFO
  OIC

  I HAS A op
  I HAS A b
  VISIBLE "OPERATOR:: " !
  GIMMEH op
  VISIBLE "SECOND NUMBER:: " !
  GIMMEH b
  VISIBLE "RESULT:: " I IZ calc YR a AN YR op AN YR b MKAY
IM OUTTA YR calculator
```

Check `quit` before conversion. Compare your result with the maintained
[`calculator.lol`](../../../samples/programs/string-calculator/calculator.lol).

## Test table

| Inputs | Expected |
| --- | --- |
| `3`, `+`, `4` | 7.00 |
| `7`, `/`, `2` | 3.50 |
| `3`, `/`, `0` | friendly zero message |
| `3`, `?`, `4` | unknown operator message |
| `quit` | loop ends without asking two more questions |

NUMBAR display uses two digits and truncation. Malformed numeric text deserves
a separate validation design; do not mistake explicit cast-to-zero behavior
for successful validation.

<details>
<summary>Extension: separate parsing from calculation</summary>

Create a function that receives already converted values, and perform input
policy in main. This makes `calc` independently testable and keeps malformed
text handling away from arithmetic.
</details>

Next, build the [adventure game](adventure.md) to manage several pieces of
state across many turns.
