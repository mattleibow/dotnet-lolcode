# FizzBuzz: a small app with a plan

FizzBuzz prints numbers from 1 to 100, except multiples of 3 say `Fizz`,
multiples of 5 say `Buzz`, and multiples of both say `FizzBuzz`. It is a small
program with an important habit: write the rules before the syntax.

1. Start `i` at 1 and stop when it reaches 101.
2. Create an empty `out` for every number.
3. If the remainder after division by 3 is zero, put `Fizz` in `out`.
4. If the remainder after division by 5 is zero, append `Buzz`.
5. Print the number only when `out` stayed empty.

```lolcode
I HAS A i ITZ 1
IM IN YR fizzbuzz UPPIN YR i TIL BOTH SAEM i AN 101
  I HAS A out ITZ ""
  BOTH SAEM MOD OF i AN 3 AN 0
  O RLY?
    YA RLY, out R "Fizz"
  OIC
  BOTH SAEM MOD OF i AN 5 AN 0
  O RLY?
    YA RLY, out R SMOOSH out AN "Buzz" MKAY
  OIC
  BOTH SAEM out AN ""
  O RLY?
    YA RLY, VISIBLE i
    NO WAI, VISIBLE out
  OIC
IM OUTTA YR fizzbuzz
```

This is the maintained
[`samples/programs/fizzbuzz/fizzbuzz.lol`](../../../samples/programs/fizzbuzz/fizzbuzz.lol).
Run it, inspect lines 3, 5, 15, and 30, then change the range to 20. Next,
add a third rule for multiples of 7 or refactor the print choice into a
function after reading [functions](../learn/functions-and-app.md).
