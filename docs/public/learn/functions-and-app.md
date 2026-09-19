# Functions, recursion, and a complete application

A function gives a repeated task a name, inputs, and optional result. It has
its own scope: pass every value it needs as a parameter rather than reaching
into the caller.

```lolcode
HOW IZ I add YR left AN YR right
  FOUND YR SUM OF left AN right
IF U SAY SO

I IZ add YR 2 AN YR 3 MKAY
VISIBLE IT
```

`I IZ` calls a function and stores its result in `IT`; `FOUND YR` returns.
Functions may call themselves, which is **recursion**. The important rule is a
base case that ends the chain, as in the repository's
[`fibonacci.lol`](../../../samples/programs/fibonacci/fibonacci.lol).

## Application walkthrough: guessing game

The [guessing game](../../../samples/games/guessing-game/guess.lol) combines the
ideas you have learned:

1. It stores a secret number, guess count, and `won` state.
2. Its `IM IN YR guessing` loop limits attempts.
3. `GIMMEH` reads text and `IS NOW A NUMBR` prepares arithmetic.
4. `BOTH SAEM` checks the answer; `BIGGR OF` identifies high guesses.
5. `GTFO` ends early on success, and `NOT won` selects the ending message.

Run it, then alter the secret and attempt limit. A next improvement is a
function that validates input before casting. Continue to
[FizzBuzz](../tutorials/fizzbuzz.md) to plan a program before coding, or open
the [language reference](../language/reference.md) for exact forms.
