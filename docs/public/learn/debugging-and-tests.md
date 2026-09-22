# 07 Finding errors and testing

**Prerequisite:** a small program with input, decisions, loops, and functions.

**Outcome:** classify a failure, reduce it to a useful example, and design a
test table containing ordinary and boundary cases.

## Errors are information

Programs can fail in different ways:

1. A **syntax or binding error** means the compiler cannot form a valid program,
   such as a missing `KTHXBYE` or unknown name. Diagnostics include a source
   location and `LOL` identifier.
2. A **runtime error** happens while valid code runs, such as arithmetic on
   nonnumeric text or division by zero.
3. A **logic error** means the program runs but gives the wrong answer. The
   compiler cannot know your intention, so tests must reveal it.

Change one thing at a time. Read the first diagnostic, inspect its line and the
line before it, form a hypothesis, make the smallest repair, and rerun.

## A program worth testing

```lolcode
HAI 1.2
  HOW IZ I category YR score
    BOTH SAEM BIGGR OF score AN 90 AN score
    O RLY?
      YA RLY, FOUND YR "GOLD"
    OIC

    BOTH SAEM BIGGR OF score AN 60 AN score
    O RLY?
      YA RLY, FOUND YR "PASS"
    OIC

    FOUND YR "RETRY"
  IF U SAY SO

  VISIBLE I IZ category YR 95 MKAY
  VISIBLE I IZ category YR 90 MKAY
  VISIBLE I IZ category YR 59 MKAY
KTHXBYE
```

Expected output:

```text
GOLD
GOLD
RETRY
```

A **test case** supplies input and states the expected result. A test table
forces you to think about boundaries:

| Score | Expected | Reason |
| ---: | --- | --- |
| 95 | GOLD | ordinary value above 90 |
| 90 | GOLD | exact upper boundary |
| 89 | PASS | just below upper boundary |
| 60 | PASS | exact lower boundary |
| 59 | RETRY | just below lower boundary |

When a loop is involved, include zero iterations, one iteration, the last
allowed iteration, and a condition that could run forever. For text input,
include empty text, expected text, and malformed numeric text. This runtime's
explicit invalid numeric casts can become zero while arithmetic coercion
throws, so decide which behavior your program accepts before writing the test.

## Practice: predict, run, explain, modify, create

1. **Predict:** What output changes if the first threshold becomes 91?
2. **Run:** Try the five table rows and record actual output.
3. **Explain:** Which rows are boundary cases, and what defect would they catch?
4. **Modify:** Introduce a logic bug by changing `60` to `61`; use the table to
   find it, then restore the code.
5. **Create:** Write a test table for the guessing game's “too low,” “too
   high,” correct-first-try, correct-last-try, and out-of-tries paths.

<details>
<summary>Hint and explained solution</summary>

Use a row for each distinct path:

| Secret | Guesses | Expected final result |
| ---: | --- | --- |
| 7 | `7` | correct in 1 try |
| 7 | `2, 7` | too low, then correct |
| 7 | `9, 7` | too high, then correct |
| 7 | `1, 2, 3, 4, 7` | correct on last try |
| 7 | `1, 2, 3, 4, 5` | out of tries, reveal 7 |

The table does not depend on implementation details. It names observable input
and output, so it remains useful after refactoring.
</details>

## Common mistakes

- Editing several lines before rerunning, which hides which change mattered.
- Testing only a comfortable middle value.
- Comparing output by memory instead of writing the expected result first.
- Treating a runtime error as “the compiler is broken” without reducing the
  input and locating the operation.
- “Fixing” a test to match wrong output instead of restating the requirement.

## Checkpoint

You can distinguish syntax, runtime, and logic failures and can write tests
around boundaries. Use [diagnostics](../language/diagnostics.md) for compiler
messages. Next: [plan a complete program](planning-programs.md).
