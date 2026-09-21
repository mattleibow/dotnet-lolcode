# 02 Values, output, and variables

**Prerequisite:** [run your first program](first-program.md).

**Outcome:** recognize LOLCODE's basic values, store them in variables, update
them, and control how output lines are assembled.

## Values are pieces of information

A program works with values: a person's name, a score, a price, or whether a
door is open. LOLCODE gives common kinds of values names:

| Kind | Plain meaning | Examples |
| --- | --- | --- |
| YARN | text | `"LOLCAT"`, `"42"` |
| NUMBR | whole number | `0`, `42`, `-7` |
| NUMBAR | number with a fractional part | `3.5`, `-0.25` |
| TROOF | true or false | `WIN`, `FAIL` |
| NOOB | no useful value yet | an uninitialized variable |

Quoted `"42"` is text; unquoted `42` is a number. They look similar to a
person but support different operations.

## Variables give values names

```lolcode
HAI 1.2
  I HAS A name ITZ "MIPS"
  I HAS A lives ITZ 9
  I HAS A sleepy ITZ WIN

  VISIBLE name " HAS " lives " LIVEZ"
  lives R 8
  VISIBLE "AFTER A NAP:: " lives
KTHXBYE
```

Expected output:

```text
MIPS HAS 9 LIVEZ
AFTER A NAP: 8
```

`I HAS A` declares a variable. `ITZ` gives it an initial value. `R` assigns a
replacement value later. The variable is the named place; the value can
change. `VISIBLE` accepts many arguments and writes their display forms
together.

The colon is LOLCODE's text escape prefix. Write `::` for a literal colon.
You will learn the other escapes in lesson 5.

## NOOB marks “not set”

```lolcode
I HAS A answer
VISIBLE answer
```

A declaration without `ITZ` begins as NOOB. That is different from the number
zero, the empty YARN `""`, and TROOF `FAIL`. Use NOOB when “not supplied yet”
is meaningful, but initialize variables promptly when a real value is required.

Assignments change a variable but do **not** update the implicit `IT` result.
Bare expression statements do update `IT`; later lessons use that distinction
for decisions.

## Practice: predict, run, explain, modify, create

1. **Predict:** What two lines are printed?

   ```lolcode
   HAI 1.2
     I HAS A points ITZ 10
     VISIBLE points
     points R 15
     VISIBLE points
   KTHXBYE
   ```

2. **Run:** Confirm that assignment affects later reads.
3. **Explain:** Point to the declaration, initializer, assignment, and reads.
4. **Modify:** Add a YARN variable named `player` and include it in both lines.
5. **Create:** Make a tiny character card containing a name, age, and one TROOF
   fact. Change one value before the final output.

<details>
<summary>Hint and explained solution</summary>

```lolcode
HAI 1.2
  I HAS A name ITZ "BYTE"
  I HAS A age ITZ 4
  I HAS A likesBoxes ITZ WIN

  VISIBLE "NAME:: " name
  VISIBLE "AGE:: " age
  VISIBLE "LIKEZ BOXEZ:: " likesBoxes
  age R 5
  VISIBLE name " WILL BE " age " NEXT"
KTHXBYE
```

Each variable has one name and a current value. The last assignment changes
only `age`; the other variables keep their values.
</details>

## Common mistakes

- Leaving YARN quotes off: `MIPS` is treated as a name, not text.
- Putting quotes around a variable name: `"name"` prints the word, while
  `name` reads the variable.
- Using `=`: LOLCODE declaration and assignment use `ITZ` and `R`.
- Treating NOOB as zero: arithmetic with NOOB is a runtime error.
- Forgetting that names are case-sensitive.

## Checkpoint

You can choose a value kind, declare a variable, replace its value, and combine
values in output. Compare your work with the maintained
[variables sample](../../../samples/basics/variables/variables.lol). Next:
[read input and calculate](values-and-decisions.md).
