# 06 Functions and a guessing game

**Prerequisite:** input, decisions, loops, and variables.

**Outcome:** define and call functions, reason about scope and return values,
then combine the course so far into an interactive guessing game.

## A function names one job

```lolcode
HAI 1.2
  HOW IZ I distanceFrom YR value AN YR target
    FOUND YR DIFF OF value AN target
  IF U SAY SO

  I IZ distanceFrom YR 9 AN YR 12 MKAY
  VISIBLE IT
KTHXBYE
```

Expected output:

```text
-3
```

`HOW IZ I` defines the function, `YR` names parameters, and `IF U SAY SO` ends
it. `I IZ ... MKAY` calls it. `FOUND YR` returns a value immediately. A call
used as a statement stores its result in the caller's `IT`.

Functions have isolated variables and their own `IT`; they cannot read an
outer program variable merely because it exists. Pass every required value as
a parameter. This makes dependencies visible and functions easier to test.

Recursion—a function calling itself—is optional enrichment. It needs a base
case that returns without another call. Explore the maintained
[recursion sample](../../../samples/programs/recursion/recursion.lol) after
ordinary calls feel comfortable.

## Build the guessing game in stages

Start with state:

```lolcode
I HAS A secret ITZ 7
I HAS A won ITZ FAIL
I HAS A attempts ITZ 0
```

Then repeat input, conversion, and a decision:

```lolcode
IM IN YR guessing UPPIN YR turn TIL BOTH SAEM turn AN 5
  I HAS A guess
  VISIBLE "GUESS 1 TO 10:: " !
  GIMMEH guess
  guess IS NOW A NUMBR
  attempts R SUM OF attempts AN 1

  BOTH SAEM guess AN secret
  O RLY?
    YA RLY
      won R WIN
      VISIBLE "CORRECT IN " attempts " TRIEZ"
      GTFO
    NO WAI
      BOTH SAEM BIGGR OF guess AN secret AN guess
      O RLY?
        YA RLY, VISIBLE "TOO HI"
        NO WAI, VISIBLE "TOO LO"
      OIC
  OIC
IM OUTTA YR guessing
```

Finally, report failure:

```lolcode
NOT won
O RLY?
  YA RLY, VISIBLE "OUTTA TRIEZ. ANSWER WUZ " secret
OIC
```

With guesses `9`, `4`, and `7`, expected interaction is:

```text
GUESS 1 TO 10: TOO HI
GUESS 1 TO 10: TOO LO
GUESS 1 TO 10: CORRECT IN 3 TRIEZ
```

The complete maintained
[guessing game](../../../samples/games/guessing-game/guess.lol) is the source
to run when your staged version works.

## Practice: predict, run, explain, modify, create

1. **Predict:** What is left in caller `IT` after `distanceFrom` returns?
2. **Run:** Call it with values whose result is positive, zero, and negative.
3. **Explain:** Why does the guessing loop keep `secret` and `won` outside the
   body?
4. **Modify:** Add a function `isCorrect` that returns
   `BOTH SAEM guess AN target`.
5. **Create:** Add a replay-free difficulty choice: 5, 8, or 10 attempts.

<details>
<summary>Hint and explained solution</summary>

```lolcode
HOW IZ I isCorrect YR guess AN YR target
  FOUND YR BOTH SAEM guess AN target
IF U SAY SO
```

Call it before `O RLY?`:

```lolcode
I IZ isCorrect YR guess AN YR secret MKAY
O RLY?
  BTW existing success and failure branches
OIC
```

The return becomes caller `IT`, exactly where `O RLY?` expects its condition.
The function depends only on its parameters, so you can test it separately.
</details>

## Common mistakes

- Reading a caller variable inside a function instead of passing a parameter.
- Forgetting `MKAY` at the end of a call with arguments.
- Assuming every path returns a value; falling off a function or using `GTFO`
  in function context returns NOOB.
- Reusing parameter names as if they updated caller variables; parameters are
  local bindings.
- Hiding all code in functions before understanding its state. Extract one
  clear job at a time.

## Checkpoint

You can identify function inputs, local state, output, and call sites, and you
have assembled an interactive application. Next:
[find errors and test behavior](debugging-and-tests.md).
