# FizzBuzz: a small app with a plan

FizzBuzz prints numbers from 1 to 100, except multiples of 3 say `Fizz`,
multiples of 5 say `Buzz`, and multiples of both say `FizzBuzz`. It is a small
program with an important habit: write the rules before the syntax.

1. Let the `UPPIN` loop create its loop-local `i` at 0 and stop before 100.
2. Turn that counter into `number` by adding 1, so the visible range is 1..100.
3. Create an empty `out` for every number.
4. Add `Fizz` and `Buzz` for the appropriate remainders.
5. Print the number only when `out` stayed empty.

```lolcode
IM IN YR fizzbuzz UPPIN YR i TIL BOTH SAEM i AN 100
  I HAS A number ITZ SUM OF i AN 1
  I HAS A out ITZ ""
  BOTH SAEM MOD OF number AN 3 AN 0
  O RLY?
    YA RLY, out R "Fizz"
  OIC
  BOTH SAEM MOD OF number AN 5 AN 0
  O RLY?
    YA RLY, out R SMOOSH out AN "Buzz" MKAY
  OIC
  BOTH SAEM out AN ""
  O RLY?
    YA RLY, VISIBLE number
    NO WAI, VISIBLE out
  OIC
IM OUTTA YR fizzbuzz
```

This is the maintained
[`samples/programs/fizzbuzz/fizzbuzz.lol`](../../../samples/programs/fizzbuzz/fizzbuzz.lol).
`UPPIN YR i` creates a fresh loop-local counter at 0; it does not preserve an
outer variable's initializer. Run the maintained sample, inspect the first,
third, fifteenth, and hundredth output lines, then change the range to 20. Next,
add a third rule for multiples of 7 or refactor the print choice into a
function after reading [functions](../learn/functions-and-app.md).
