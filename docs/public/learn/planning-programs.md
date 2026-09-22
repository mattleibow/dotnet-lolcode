# 08 Planning a complete program

**Prerequisite:** functions, loops, decisions, and basic tests.

**Outcome:** turn a problem into state, actions, functions, and testable steps
before writing the full program.

## Plan the changing facts

Consider a quiz with three questions. Before syntax, write the facts that
change:

- current score;
- current question;
- answer entered by the player.

Those facts are **state**. Then list the actions:

1. show a question;
2. read an answer;
3. decide whether it matches;
4. update the score;
5. continue;
6. report the final result.

The plan exposes two useful functions: one that compares an answer and one that
prints the final message. Keep input and mutable score in the main program so
their lifetime remains obvious.

## A complete small plan in code

```lolcode
HAI 1.2
  HOW IZ I correct YR actual AN YR expected
    FOUND YR BOTH SAEM actual AN expected
  IF U SAY SO

  I HAS A score ITZ 0
  I HAS A answer

  VISIBLE "WAT IZ 2 + 2? " !
  GIMMEH answer
  I IZ correct YR answer AN YR "4" MKAY
  O RLY?
    YA RLY, score R SUM OF score AN 1
  OIC

  VISIBLE "TYPE LOL IN CAPS:: " !
  GIMMEH answer
  I IZ correct YR answer AN YR "LOL" MKAY
  O RLY?
    YA RLY, score R SUM OF score AN 1
  OIC

  VISIBLE "SCORE:: " score "/2"
KTHXBYE
```

With input `4` then `LOL`, expected output ends with:

```text
SCORE: 2/2
```

Notice that `correct` has no hidden dependency. Its two parameters are enough
to test it. The main program owns the changing score.

## Decompose by responsibility

A function is useful when it:

- answers one clear question;
- transforms inputs into one result;
- removes repeated logic;
- gives a meaningful name to a step.

Do not split every line into a function. A function called `doThing` that reads
many unrelated globals makes the plan harder, not easier. In this compiler,
normal functions cannot access outer variables anyway, which encourages
explicit inputs.

Before coding a larger program, make four lists:

| List | Question |
| --- | --- |
| State | What facts change over time? |
| Inputs/outputs | What enters and leaves the program? |
| Actions/functions | What named jobs transform the state? |
| Tests | What ordinary, boundary, and failure paths prove it works? |

## Practice: predict, run, explain, modify, create

1. **Predict:** What score results from `4` then `lol`?
2. **Run:** Confirm equality is case-sensitive.
3. **Explain:** Why is score kept in the main program?
4. **Modify:** Add a third question and update the denominator.
5. **Create:** Plan a tiny shopping total program. List its state, inputs,
   actions, and five tests before writing code.

<details>
<summary>Hint and explained solution</summary>

One possible plan:

- State: running total and item count.
- Input: three item prices as NUMBAR-compatible text.
- Actions: convert each price, add it, then calculate an average.
- Output: total and average.
- Tests: all positive values; a zero price; fractional prices; one malformed
  value; and a zero-item version if the design later allows an empty basket.

The next step is not code. Decide what malformed input should do. That decision
changes both the program and its tests.
</details>

## Common mistakes

- Starting with syntax before deciding what the program must remember.
- Mixing input, calculation, and display in every function.
- Creating state that is never read or changed.
- Hiding dependencies rather than passing parameters.
- Planning only the success path.

## Checkpoint

You can describe a program independently of LOLCODE syntax, then map the plan
to variables, functions, and tests. Next, take an optional development step
into [objects and program state](objects-and-state.md), or skip to
[project workflow](project-workflow.md) to remain on stable 1.2.
