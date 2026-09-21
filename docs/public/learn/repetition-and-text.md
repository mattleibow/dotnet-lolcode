# 05 Repetition and text

**Prerequisite:** variables, comparisons, and decisions.

**Outcome:** design a terminating loop, avoid off-by-one errors, and construct
YARN values with escapes, interpolation, and `SMOOSH`.

## A loop repeats a body

This loop prints 0 through 4:

```lolcode
HAI 1.2
  IM IN YR count UPPIN YR i TIL BOTH SAEM i AN 5
    VISIBLE i
  IM OUTTA YR count
KTHXBYE
```

Expected output:

```text
0
1
2
3
4
```

The loop creates a fresh NUMBR counter `i` initialized to zero. Before each
iteration, `TIL` asks whether its condition is true; `WILE` instead continues
while its condition is true. After the body, `UPPIN` adds one (`NERFIN`
subtracts one). The opening and closing loop names must match.

This order explains two common bugs:

- **Off by one:** stopping at 5 means 5 is not printed.
- **Infinite loop:** if the changing value can never satisfy the stop
  condition, the loop never ends.

`GTFO` exits the nearest loop immediately. In 1.2, each iteration has a child
scope for declarations but its final `IT` propagates outward; if the body runs
zero times, the previous outer `IT` is preserved. In 1.3/1.4 the loop child
scope does not leak declarations or `IT`.

## Text is data too

```lolcode
HAI 1.2
  I HAS A cat ITZ "MIPS"
  I HAS A lives ITZ 9
  VISIBLE "NAME:: :{cat}:)LIVEZ:: :{lives}"
  VISIBLE SMOOSH cat AN " SEZ :"OH HAI!:"" MKAY
KTHXBYE
```

Expected output:

```text
NAME: MIPS
LIVEZ: 9
MIPS SEZ "OH HAI!"
```

The colon always starts a source-string escape:

| Escape | Meaning |
| --- | --- |
| `:)` | newline |
| `:>` | tab |
| `::` | literal colon |
| `:"` | quote |
| `:o` | bell |
| `:{name}` | interpolate a variable |

`SMOOSH ... MKAY` produces one YARN from several values. Use interpolation for
short templates and `SMOOSH` when values are assembled conditionally.

## Practice: predict, run, explain, modify, create

1. **Predict:** What are the first and last values printed by the counter?
2. **Run:** Confirm the stop condition is checked before the body.
3. **Explain:** Change `TIL BOTH SAEM i AN 5` to 4. Why is one line removed?
4. **Modify:** Print `SMOOSH "ROUND " AN i MKAY` in each iteration.
5. **Create:** Print a triangle containing one, two, three, then four `*`
   characters.

<details>
<summary>Hint and explained solution</summary>

```lolcode
HAI 1.2
  I HAS A row ITZ ""
  IM IN YR triangle UPPIN YR i TIL BOTH SAEM i AN 4
    row R SMOOSH row AN "*" MKAY
    VISIBLE row
  IM OUTTA YR triangle
KTHXBYE
```

`row` lives outside the loop and keeps its previous text. Each iteration adds
one star before printing. The loop counter still begins at zero, but this
program uses it only to stop after four iterations.
</details>

## Common mistakes

- Assuming the counter begins at a previously assigned value; the loop counter
  is a new zero-initialized local.
- Using `TIL` when you mean “continue while”; write the condition in words
  before choosing `TIL` or `WILE`.
- Forgetting that `GTFO` skips the update step.
- Writing a literal colon as `:` instead of `::`.
- Omitting `MKAY` where a variadic expression shares a line with more syntax.

## Checkpoint

You can state a loop's initial value, guard, body, update, and termination
condition. Run the maintained [loops](../../../samples/basics/loops/loops.lol)
and [strings](../../../samples/basics/string-ops/strings.lol) samples. Next:
[functions and a guessing game](functions-and-app.md).
