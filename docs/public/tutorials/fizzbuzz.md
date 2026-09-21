# Build FizzBuzz in stages

**You need:** variables, loops, remainder, and decisions.

**You will build:** a stable 1.2 program that prints 1–100, replacing multiples
of 3 and 5 with words.

## 1. Write the rules before code

For each number:

1. start with empty output;
2. append `Fizz` when divisible by 3;
3. append `Buzz` when divisible by 5;
4. print the number only when output stayed empty.

This order naturally handles 15: both words are appended.

## 2. Produce exactly 1 through 100

A LOLCODE loop counter starts at zero. Convert it to the visible number:

```lolcode
IM IN YR fizzbuzz UPPIN YR i TIL BOTH SAEM i AN 100
  I HAS A number ITZ SUM OF i AN 1
  VISIBLE number
IM OUTTA YR fizzbuzz
```

Run it now. Check the first and last lines before adding rules. If the last line
is 99 or 101, repair the range before continuing.

## 3. Detect one divisor

`MOD OF number AN 3` is zero exactly when 3 divides the number:

```lolcode
BOTH SAEM MOD OF number AN 3 AN 0
O RLY?
  YA RLY, VISIBLE "Fizz"
  NO WAI, VISIBLE number
OIC
```

Test 2, 3, and 6 mentally, then run a shortened 1–10 loop.

## 4. Compose both rules

Use an output YARN rather than printing immediately:

```lolcode
I HAS A out ITZ ""

BOTH SAEM MOD OF number AN 3 AN 0
O RLY?
  YA RLY, out R "Fizz"
OIC

BOTH SAEM MOD OF number AN 5 AN 0
O RLY?
  YA RLY, out R SMOOSH out AN "Buzz" MKAY
OIC
```

The second rule appends instead of replacing, so 15 becomes `FizzBuzz`.

## 5. Choose number or word

```lolcode
BOTH SAEM out AN ""
O RLY?
  YA RLY, VISIBLE number
  NO WAI, VISIBLE out
OIC
```

Put all stages together or compare with the maintained
[`fizzbuzz.lol`](../../../samples/programs/fizzbuzz/fizzbuzz.lol).

## Verify before extending

| Number | Expected |
| ---: | --- |
| 1 | 1 |
| 3 | Fizz |
| 5 | Buzz |
| 15 | FizzBuzz |
| 98 | 98 |
| 99 | Fizz |
| 100 | Buzz |

Modify the stop value to 20 while debugging. Then restore 100.

<details>
<summary>Extension: add multiples of 7</summary>

Append `Pop` when `MOD OF number AN 7` is zero. Predict 21, 35, and 105 before
running. The existing empty-output decision requires no change because the new
rule participates in the same composition.
</details>

## What this walkthrough teaches

Plan rules, prove the range, add one condition, compose conditions, then test
boundaries. That staged method matters more than FizzBuzz itself. Continue with
the [calculator](calculator.md) for input and function boundaries.
