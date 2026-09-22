# 03 Input and calculations

**Prerequisite:** variables and basic value kinds.

**Outcome:** read terminal input, convert YARNs deliberately, build prefix
arithmetic expressions, and display a calculated result.

## Input begins as text

`GIMMEH variable` waits for one input line and stores it as a YARN. Even when a
person types `12`, the program initially receives the text `"12"`. Converting
it makes your intention explicit.

```lolcode
HAI 1.2
  I HAS A age
  VISIBLE "HOW OLD R U? " !
  GIMMEH age
  age IS NOW A NUMBR
  I HAS A nextAge ITZ SUM OF age AN 1
  VISIBLE "NEXT BIRTHDAY U WILL BE " nextAge
KTHXBYE
```

Input:

```text
12
```

Expected output:

```text
HOW OLD R U? NEXT BIRTHDAY U WILL BE 13
```

The `!` suppresses `VISIBLE`'s normal newline, so the cursor remains after the
question. `IS NOW A` converts and replaces the variable. `MAEK age A NUMBR`
would produce a converted value without replacing `age`.

## Operators come before their inputs

Many languages write `age + 1`. LOLCODE uses a **prefix operator**:
`SUM OF age AN 1`. Read it as “the sum of age and one.”

| Operation | Form |
| --- | --- |
| add | `SUM OF left AN right` |
| subtract | `DIFF OF left AN right` |
| multiply | `PRODUKT OF left AN right` |
| divide | `QUOSHUNT OF left AN right` |
| remainder | `MOD OF left AN right` |
| larger/smaller | `BIGGR OF ...`, `SMALLR OF ...` |

Nested expressions are evaluated from the inside out:

```lolcode
I HAS A total ITZ SUM OF PRODUKT OF 3 AN 4 AN 5
VISIBLE total
```

Expected output is `17`: first `3 * 4`, then `12 + 5`.

Explicit casts and arithmetic coercion are intentionally different. An
explicit invalid numeric YARN cast follows the documented prefix/zero behavior
and can produce zero; a nonnumeric YARN used directly in arithmetic raises a
runtime error. Validate important input rather than treating zero as proof that
the person typed zero. NUMBAR display as YARN truncates toward zero to two
decimal places.

## Practice: predict, run, explain, modify, create

1. **Predict:** What is `DIFF OF PRODUKT OF 5 AN 3 AN 2`?
2. **Run:** Print the expression and compare.
3. **Explain:** Draw parentheses showing the evaluation order.
4. **Modify:** Ask for width and height, convert both to NUMBR, and print their
   product.
5. **Create:** Build a bill splitter that reads a NUMBAR total and a NUMBR
   person count, then prints `QUOSHUNT OF total AN people`.

<details>
<summary>Hint and explained solution</summary>

```lolcode
HAI 1.2
  I HAS A total
  I HAS A people
  VISIBLE "TOTAL? " !
  GIMMEH total
  VISIBLE "HOW MANY PPL? " !
  GIMMEH people

  total IS NOW A NUMBAR
  people IS NOW A NUMBR
  VISIBLE "EACH PAYS " QUOSHUNT OF total AN people
KTHXBYE
```

Both inputs arrive as YARNs. The casts prepare numeric operations. This first
version assumes valid input and a nonzero person count; lesson 4 adds decisions
and lesson 7 adds test cases for those risks.
</details>

## Common mistakes

- Writing infix math such as `2 + 3`; use `SUM OF 2 AN 3`.
- Forgetting to convert `GIMMEH` input before arithmetic.
- Losing track of nesting; name intermediate values while learning.
- Dividing by zero. This runtime reports division and modulo by zero as
  `LolRuntimeException`.
- Expecting NUMBAR output to preserve every input digit; YARN display truncates
  to two decimal places.

## Checkpoint

You can turn input text into numeric values and build a calculation. Explore
the maintained [math](../../../samples/basics/math/math.lol) and
[casting](../../../samples/basics/casting/casting.lol) samples. Next:
[make decisions](decisions.md).
